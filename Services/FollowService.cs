using Microsoft.Data.SqlClient;

namespace MiniSocial.Services
{
    public class FollowService
    {
        private readonly string _connectionString;


        public FollowService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
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
    }
}
