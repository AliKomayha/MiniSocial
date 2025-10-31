
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MiniSocial.Dtos;
using MiniSocial.Models;
using Microsoft.AspNetCore.Authorization;

namespace MiniSocial.Repositories
{
    public class FeedRepository
    {
        private readonly string _connectionString;

        public FeedRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public List<PostDto> GetFeedPosts(int currentUserId, int offset, int limit) {
            
            var posts = new List<PostDto>();
            

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var followedUserIds = new List<int>();
                var followCmd =new SqlCommand(@"SELECT FollowingId 
                                                FROM Follows 
                                                WHERE FollowerId = @UserId 
                                                AND Status = 'Accepted'", connection);

                followCmd.Parameters.AddWithValue("@UserId", currentUserId); // Replace with actual user ID

                using (var reader = followCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        followedUserIds.Add(reader.GetInt32(0));
                    }
                }

                var userIds = followedUserIds.Any() ? string.Join(",", followedUserIds) : currentUserId.ToString();

                // fetch posts 
                 var query =$@"
                    SELECT p.Id, p.UserId, p.Text, u.UserName, pr.DisplayName, pr.Avatar, pr.IsPrivate,
                           p.ImagePath, p.CreatedAt,
                           (SELECT COUNT(*) FROM Likes WHERE PostId = p.Id) AS LikeCount,
                           (SELECT COUNT(*) FROM Comments WHERE PostId = p.Id) AS CommentCount
                    FROM Posts p
                    JOIN Users u ON p.UserId = u.Id
                    JOIN Profiles pr ON u.Id = pr.UserId
                    WHERE (
                        p.UserId = @UserId
                        OR (
                            p.UserId IN ({userIds})
                        AND (
                              pr.IsPrivate = 0
                                OR EXISTS (
                                        SELECT 1 FROM Follows f
                                        WHERE f.FollowerId = @UserId
                                        AND f.FollowingId = p.UserId
                                        AND f.Status = 'Accepted'
                                    )
                                )
                            )
                        )
                        
                    ORDER BY p.CreatedAt DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @Limit ROWS ONLY;
               ";
                var postCmd = new SqlCommand(query, connection);
                postCmd.Parameters.AddWithValue("@UserId", currentUserId); 
                postCmd.Parameters.AddWithValue("@Offset", offset); // For pagination
                postCmd.Parameters.AddWithValue("@Limit", limit); // For pagination
                using (var reader = postCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var post = new PostDto
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            Text = reader.GetString(2),
                            UserName = reader.GetString(3),
                            DisplayName = reader.GetString(4),
                            Avatar = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsPrivate = reader.GetBoolean(6),
                            ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            LikeCount = reader.GetInt32(9),
                            CommentCount = reader.GetInt32(10)
                        };
                        posts.Add(post);
                    }
                }
            }
                
                
            return posts;
            
        }

        public List<PostDto> GetForYouFeedPosts(int currentUserId, int offset, int limit)
        {
            var posts = new List<PostDto>();

            // select posts from follow and public users 

            return posts;
        }


    }
}
