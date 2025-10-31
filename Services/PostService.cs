using Microsoft.Data.SqlClient;
using MiniSocial.Dtos;
using MiniSocial.Models;

namespace MiniSocial.Services
{
    public class PostService
    {
        private readonly string _connectionString;


        public PostService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public void CreatePost(Post post)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"INSERT INTO Posts (UserId, Text, ImagePath) 
                                            VALUES(@UserId, @Text, @ImagePath)", connection);

                command.Parameters.AddWithValue("@UserId", post.UserId);
                command.Parameters.AddWithValue("@Text", post.Text);
                command.Parameters.AddWithValue("@ImagePath", (object?)post.ImagePath ?? DBNull.Value);
                command.ExecuteNonQuery();

            }
        }

        public bool DeletePost(int postId, int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"DELETE Posts WHERE Id=@Id AND UserId=@UserId", connection);
                command.Parameters.AddWithValue("@Id", postId);
                command.Parameters.AddWithValue("@UserId", userId);
                

                return command.ExecuteNonQuery() > 0;
            }

            
        }

        public bool UpdatePost(Post post)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();


                var command = new SqlCommand(@"
                        UPDATE Posts
                        SET Text = @Text,
                        ImagePath = @ImagePath
                            
                        WHERE Id = @PostId
                        AND UserId = @UserId

                        ", connection);

                command.Parameters.AddWithValue("@Text", post.Text);
                command.Parameters.AddWithValue("@ImagePath", post.ImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PostId", post.Id);
                command.Parameters.AddWithValue("@UserId", post.UserId);
                
                return command.ExecuteNonQuery() > 0;
            }
        }

        public List<PostDto> GetPosts(int UserId)
        {
            var posts = new List<PostDto>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(@"
                    SELECT p.Id, p.Text, p.ImagePath, p.CreatedAt, u.Id, u.UserName, pr.DisplayName, pr.Avatar,
                    (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount,
                    (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount
                    FROM Posts p
                    JOIN Users u ON p.UserId = u.Id
                    JOIN Profiles pr ON u.Id = pr.UserId
                    WHERE u.Id = @UserId
                    ORDER BY p.CreatedAt DESC", connection);

                command.Parameters.AddWithValue("@UserId", UserId);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var post = new PostDto
                    {
                        Id = reader.GetInt32(0),
                        Text = reader.GetString(1),
                        ImagePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                        CreatedAt = reader.GetDateTime(3),
                        UserId = reader.GetInt32(4),
                        UserName = reader.GetString(5),
                        DisplayName = reader.GetString(6),
                        Avatar = reader.IsDBNull(7) ? null : reader.GetString(7),
                        LikeCount = reader.GetInt32(8),
                        CommentCount = reader.GetInt32(9)
                    };
                    posts.Add(post);
                }

                return posts;
            }

        }

        public PostDto? GetPostById(int postId)
        {
            PostDto? postDto = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"
                   SELECT p.Id, p.Text, p.ImagePath, p.CreatedAt,
                   u.Id, u.UserName, pr.DisplayName, pr.Avatar,
                   (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount,
                   (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount
                    FROM Posts p
                    JOIN Users u ON p.UserId = u.Id
                    JOIN Profiles pr ON u.Id = pr.UserId
                    WHERE p.Id = @PostId
                   ", connection);

                command.Parameters.AddWithValue("@PostId", postId);

                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    postDto = new PostDto
                    {
                        Id = reader.GetInt32(0),
                        Text = reader.GetString(1),
                        ImagePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                        CreatedAt = reader.GetDateTime(3),
                        UserId = reader.GetInt32(4),
                        UserName = reader.GetString(5),
                        DisplayName = reader.GetString(6),
                        Avatar = reader.IsDBNull(7) ? null : reader.GetString(7),
                        LikeCount = reader.GetInt32(8),
                        CommentCount = reader.GetInt32(9)

                    };
                    
                }
            }
            return postDto;
        }

        public Post? GetPostEntityById(int postId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT Id, UserId, Text, ImagePath, CreatedAt FROM Posts WHERE Id=@PostId", connection);
            command.Parameters.AddWithValue("@PostId", postId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Post
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Text = reader.GetString(2),
                    ImagePath = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                };
            }
            return null;
        }


        public void ToggleLike(int UserId, int PostId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Check if liked
                var checkCommand = new SqlCommand("SELECT COUNT(*) FROM Likes WHERE UserId=@UserId AND PostId=@PostId", connection);
                checkCommand.Parameters.AddWithValue("@UserId", UserId);
                checkCommand.Parameters.AddWithValue("@PostId", PostId);
                bool exists = (int)checkCommand.ExecuteScalar() > 0;

                if (exists)
                {
                    // Ulike
                    var deleteCommand = new SqlCommand("DELETE FROM Likes WHERE UserId=@UserId AND PostId=@PostId", connection);
                    deleteCommand.Parameters.AddWithValue("@UserId", UserId);
                    deleteCommand.Parameters.AddWithValue("@PostId", PostId);
                    deleteCommand.ExecuteNonQuery();
                }
                else
                {
                    // Like
                    var insertCommand = new SqlCommand(@"
                        IF NOT EXISTS (SELECT 1 FROM Likes WHERE UserId=@UserId AND PostId=@PostId)
                        BEGIN
                        INSERT INTO Likes (UserId, PostId) VALUES(@UserId, @PostId)
                        END", connection);
                    insertCommand.Parameters.AddWithValue("@UserId", UserId);
                    insertCommand.Parameters.AddWithValue("@PostId", PostId);
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        public int GetLikeCount(int postId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand("SELECT COUNT(*) FROM Likes WHERE PostId = @PostId", connection);
                command.Parameters.AddWithValue("@PostId", postId);

                return (int)command.ExecuteScalar();
            }
        }





    }
}
