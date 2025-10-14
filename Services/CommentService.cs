using Microsoft.Data.SqlClient;
using MiniSocial.Dtos;
using MiniSocial.Models;

namespace MiniSocial.Services
{
    public class CommentService
    {
        private readonly string _connectionString;


        public CommentService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public void AddComment(Comment comment)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
            INSERT INTO Comments (PostId, UserId, Text, ParentCommentId)
            VALUES (@PostId, @UserId, @Text, @ParentCommentId)", connection);

            command.Parameters.AddWithValue("@PostId", comment.PostId);
            command.Parameters.AddWithValue("@UserId", comment.UserId);
            command.Parameters.AddWithValue("@Text", comment.Text);
            command.Parameters.AddWithValue("@ParentCommentId", (object?)comment.ParentCommentId ?? DBNull.Value);

            command.ExecuteNonQuery();
        }

        public List<CommentDto> GetCommentsByPost(int postId)
        {
            var comments = new List<CommentDto>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
            SELECT c.Id, c.PostId, c.UserId, u.UserName, p.DisplayName, p.Avatar, c.Text, c.ParentCommentId, c.CreatedAt
            FROM Comments c
            JOIN Users u ON c.UserId = u.Id
            JOIN Profiles p ON u.Id = p.UserId
            WHERE c.PostId = @PostId
            ORDER BY c.CreatedAt ASC", connection);

            command.Parameters.AddWithValue("@PostId", postId);

            var reader = command.ExecuteReader();
            var lookup = new Dictionary<int, CommentDto>();

            while (reader.Read())
            {
                var comment = new CommentDto
                {
                    Id = reader.GetInt32(0),
                    PostId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    UserName = reader.GetString(3),
                    DisplayName = reader.GetString(4),
                    Avatar = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Text = reader.GetString(6),
                    ParentCommentId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    CreatedAt = reader.GetDateTime(8)
                };

                lookup[comment.Id] = comment;

                if (comment.ParentCommentId.HasValue && lookup.ContainsKey(comment.ParentCommentId.Value))
                {
                    lookup[comment.ParentCommentId.Value].Replies.Add(comment);
                }
                else
                {
                    comments.Add(comment);
                }
            }

            return comments;
        }


        public void DeleteComment(int commentId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("DELETE FROM Comments WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", commentId);
            command.ExecuteNonQuery();

        }
    }
}
