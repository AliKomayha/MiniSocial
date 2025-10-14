using Microsoft.Data.SqlClient;
using MiniSocial.Models;

namespace MiniSocial.Services
{
    public class FollowService
    {
        private readonly string _connectionString;
        private readonly ProfileService _profileService;


        public FollowService(IConfiguration config, ProfileService profileService)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _profileService = profileService;
        }

        public bool IsApprovedFollower(int followerId, int followingId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM Follows WHERE FollowerId=@FollowerId AND FollowingId=@FollowingId AND Status='Accepted'",
                    connection);

                command.Parameters.AddWithValue("@FollowerId", followerId);
                command.Parameters.AddWithValue("@FollowingId", followingId);

                return (int)command.ExecuteScalar() > 0;
            }
        }

        public string RequestFollow(int followerId, int followingId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

           Profile profile = _profileService.GetProfileByUserId(followingId);
            bool isPrivate = profile.IsPrivate;

            string status = isPrivate ? "Pending" : "Accepted";

            var command = new SqlCommand(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM Follows 
                        WHERE FollowerId=@FollowerId AND FollowingId=@FollowingId)
                BEGIN
                    INSERT INTO Follows (FollowerId, FollowingId, Status, CreatedAt)
                    VALUES (@FollowerId, @FollowingId, @Status, GETDATE())
                END
                ELSE
                BEGIN
                    UPDATE Follows 
                    SET Status=@Status 
                    WHERE FollowerId=@FollowerId AND FollowingId=@FollowingId
                END
                ", connection);

            command.Parameters.AddWithValue("@FollowerId", followerId);
            command.Parameters.AddWithValue("@FollowingId", followingId);
            command.Parameters.AddWithValue("@Status", status);
            command.ExecuteNonQuery();

            return status;

        }

        public void UnFollow(int followerId, int followingId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                  DELETE FROM Follows 
                  WHERE FollowerId=@FollowerId AND FollowingId=@FollowingId 
                ", connection);

            command.Parameters.AddWithValue("@FollowerId", followerId);
            command.Parameters.AddWithValue("@FollowingId", followingId);
            command.ExecuteNonQuery();

        }

        public void ApproveFollow (int followId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("UPDATE Follows SET Status='Accepted' WHERE Id=@Id", connection);
            command.Parameters.AddWithValue("@Id", followId);
            command.ExecuteNonQuery();
        }

        public void RejectFollow( int followId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("DELETE Follows WHERE Id=@Id", connection);
            command.Parameters.AddWithValue("@Id", followId);
            command.ExecuteNonQuery();

        }

        public string GetFollowStatus(int followerId, int followingId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                SELECT Status FROM Follows 
                WHERE FollowerId=@FollowerId AND FollowingId=@FollowingId
            ", connection);

            command.Parameters.AddWithValue("@FollowerId", followerId);
            command.Parameters.AddWithValue("@FollowingId", followingId);

            var result = command.ExecuteScalar() as string;
            return result ?? "None"; // None, Pending, Accepted
        }


        public List<(int Id, int FollowerId)> GetPendingRequests(int userId)
        {
            var list = new List<(int, int)>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                SELECT Id, FollowerId 
                FROM Follows 
                WHERE FollowingId=@UserId AND Status='Pending'
            ", connection);

            command.Parameters.AddWithValue("@UserId", userId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }

            return list;
        }



        public List<Profile> GetFollowers(int userId)
        {
            var list = new List<Profile>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                SELECT Users.Id, Users.UserName, Profiles.DisplayName, Profiles.Avatar
                FROM Follows
                JOIN Users ON Users.Id = Follows.FollowerId
                JOIN Profiles ON Profiles.UserId = Users.Id
                WHERE Follows.FollowingId=@UserId AND Follows.Status='Accepted'
    ", connection);

            command.Parameters.AddWithValue("@UserId", userId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Profile
                {
                    UserId = reader.GetInt32(0),
                    User = new User { Id = reader.GetInt32(0), Username = reader.GetString(1) },
                    DisplayName = reader.GetString(2),
                    Avatar = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return list;
        }

        public List<Profile> GetFollowing(int userId)
        {
            var list = new List<Profile>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                SELECT Users.Id, Users.UserName, Profiles.DisplayName, Profiles.Avatar
                FROM Follows
                JOIN Users ON Users.Id = Follows.FollowingId
                JOIN Profiles ON Profiles.UserId = Users.Id
                WHERE Follows.FollowerId=@UserId AND Follows.Status='Accepted'
    ", connection);

            command.Parameters.AddWithValue("@UserId", userId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Profile
                {
                    UserId = reader.GetInt32(0),
                    User = new User { Id = reader.GetInt32(0), Username = reader.GetString(1) },
                    DisplayName = reader.GetString(2),
                    Avatar = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return list;
        }



    }
}



