using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniSocial.Dtos;
using MiniSocial.Models;
using MiniSocial.Services;

namespace MiniSocial.ApiControllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PostsApiController : ControllerBase
    {

        private readonly PostService _postService;
        private readonly CommentService _commentService;

        public PostsApiController(PostService postService, CommentService commentService)
        {
            _postService = postService;
            _commentService = commentService;
        }

        // CREATE POST
        [HttpPost]
        public IActionResult Create([FromForm] string text, [FromForm] IFormFile? image)
        {
            var userId = int.Parse(User.FindFirstValue("userId"));
            if (userId == 0) return Unauthorized();

            string? imagePath = null;
            if (image != null)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine("wwwroot/uploads/posts", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    image.CopyTo(stream);
                imagePath = "/uploads/posts/" + fileName;
            }

            var post = new Post
            {
                UserId = userId,
                Text = text,
                ImagePath = imagePath
            };

            _postService.CreatePost(post);

            return Ok(new { success = true, post });
        }

        // GET USER POSTS
        [HttpGet]
        public IActionResult UserPosts([FromQuery] int userId)
        {
            var posts = _postService.GetPosts(userId);
            return Ok(posts);
        }

        // EDIT POST
        [HttpPost]
        public IActionResult Edit([FromForm] int id, [FromForm] string text, [FromForm] IFormFile? image)
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var existingPost = _postService.GetPostEntityById(id);
            if (existingPost == null || existingPost.UserId != currentUserId)
                return NotFound();

            existingPost.Text = text;

            if (image != null && image.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var path = Path.Combine("wwwroot/uploads/posts", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                    image.CopyTo(stream);
                existingPost.ImagePath = "/uploads/posts/" + fileName;
            }

            var updated = _postService.UpdatePost(existingPost);

            return updated ? Ok(new { success = true, post = existingPost }) : BadRequest("Failed to update post.");
        }

        // DELETE POST
        [HttpPost]
        public IActionResult Delete([FromForm] int postId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!_postService.DeletePost(postId, userId))
                return BadRequest("Cannot delete this post.");

            return Ok(new { success = true });
        }

        // TOGGLE LIKE
        [HttpPost]
        public IActionResult ToggleLike([FromForm] int postId)
        {
            var userId = int.Parse(User.FindFirstValue("userId"));
            Console.WriteLine("userId from token: " + userId);

            _postService.ToggleLike(userId, postId);
            int updatedCount = _postService.GetLikeCount(postId);

            return Ok(new { success = true, likeCount = updatedCount });
        }

        // GET COMMENTS
        [HttpGet]
        public IActionResult GetComments([FromQuery] int postId)
        {
            var comments = _commentService.GetCommentsByPost(postId);
            return Ok(comments);
        }

        // ADD COMMENT
        [HttpPost]
        public IActionResult AddComment([FromForm] int postId, [FromForm] string text, [FromForm] int? parentCommentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            _commentService.AddComment(new Comment
            {
                PostId = postId,
                UserId = userId,
                Text = text,
                ParentCommentId = parentCommentId
            });

            var updatedComments = _commentService.GetCommentsByPost(postId);
            return Ok(updatedComments);
        }

        // DELETE COMMENT
        [HttpPost]
        public IActionResult DeleteComment([FromForm] int commentId, [FromForm] int postId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var comment = _commentService.GetCommentsByPost(postId)
                                         .SelectMany(c => FlattenComments(c))
                                         .FirstOrDefault(c => c.Id == commentId);

            if (comment == null) return NotFound();
            if (comment.UserId != userId) return Forbid();

            _commentService.DeleteComment(commentId);
            var updatedComments = _commentService.GetCommentsByPost(postId);
            return Ok(updatedComments);
        }

        // Helper to flatten replies recursively
        private IEnumerable<CommentDto> FlattenComments(CommentDto comment)
        {
            yield return comment;
            foreach (var reply in comment.Replies)
                foreach (var c in FlattenComments(reply))
                    yield return c;
        }



    }
}
