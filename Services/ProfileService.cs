using System.Security.Claims;
using Microsoft.Data.SqlClient;
using MiniSocial.Dtos;
using MiniSocial.Models;

namespace MiniSocial.Services
{
    public class ProfileService
    {
        private readonly string _connectionString;


        public ProfileService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public Profile? GetProfileByUserId(int userId)
        {
            Profile? profile = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"
                    SELECT p.Id, p.DisplayName, p.Avatar, p.Bio, p.BirthDate, p.IsPrivate, u.Id, u.UserName
                    FROM Profiles p
                    JOIN Users u ON u.Id = p.UserId
                    WHERE u.Id = @UserId", connection);

                command.Parameters.AddWithValue("@UserId", userId);

                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    profile = new Profile
                    {
                            Id = reader.GetInt32(0),
                            DisplayName = reader.GetString(1),
                            Avatar = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Bio = reader.IsDBNull(3) ? null : reader.GetString(3),
                            BirthDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                            IsPrivate = reader.GetBoolean(5),
                            UserId = reader.GetInt32(6),
                            User = new User
                            {
                                Id = reader.GetInt32(6),
                                Username = reader.GetString(7)
                            }
                    };
                }
            }
            return profile;
        }



        public bool UpdateProfile(Profile profile)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"
                        UPDATE Profiles
                        SET DisplayName = @DisplayName,
                            Bio = @Bio,
                            Avatar = @Avatar,
                            BirthDate = @BirthDate,
                            IsPrivate = @IsPrivate
                        WHERE UserId = @UserId", connection);


                command.Parameters.AddWithValue("@DisplayName", profile.DisplayName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Bio", profile.Bio ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Avatar", profile.Avatar ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BirthDate", profile.BirthDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IsPrivate", profile.IsPrivate);
                command.Parameters.AddWithValue("@UserId", profile.UserId);

                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0;

            }


        }




        public List<SearchProfileDto> SearchProfiles(string query)
        {
            var results = new List<SearchProfileDto>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                    SELECT p.Id, p.DisplayName, u.Username, p.Avatar
                    FROM Profiles p
                    JOIN Users u ON u.Id = p.UserId
                    WHERE p.DisplayName LIKE @Query OR u.Username LIKE @Query
                ", connection);

            command.Parameters.AddWithValue("@Query", $"%{query}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new SearchProfileDto
                {
                    Id = reader.GetInt32(0),
                    DisplayName = reader.GetString(1),
                    Username = reader.GetString(2),
                    Avatar = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return results;
        }





    }
}
