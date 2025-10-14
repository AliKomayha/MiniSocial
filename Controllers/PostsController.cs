using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MiniSocial.Dtos;
using MiniSocial.Models;
using MiniSocial.Services;

namespace MiniSocial.Controllers
{
    public class PostsController : Controller
    {
        private readonly PostService _postService;
        private readonly CommentService _commentService;

        public PostsController(PostService postService, CommentService commentService)
        {
            _postService = postService;
            _commentService = commentService;
        }
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(string text, IFormFile? image)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            string? imagePath = null;
            if (image != null)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine("wwwroot/uploads/posts", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }
                imagePath = "/uploads/posts/" + fileName;
            }
            _postService.CreatePost( new Post

            {
                UserId = userId,
                Text = text,
                ImagePath = imagePath
            });
            return RedirectToAction("ViewProfile", "Profiles", new { id = userId });
        }


        [HttpGet]
        public IActionResult UserPosts(int UserId)
        {
            var posts = _postService.GetPosts(UserId);
            return View(posts);
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var post = _postService.GetPostById(id);
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (post == null || post.UserId != currentUserId)
                return NotFound();

            return View(post);
        }

        [HttpPost]
        public IActionResult Edit(Post post)
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            post.UserId = currentUserId;

            var existingPost = _postService.GetPostById(post.Id); // get current post from DB
            if (existingPost == null || existingPost.UserId != currentUserId)
                return NotFound();


            // handle image upload if new file is provided
            if (post.ImageFile != null && post.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(post.ImageFile.FileName);
                var path = Path.Combine("wwwroot/uploads/posts", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    post.ImageFile.CopyTo(stream);
                }
                post.ImagePath = "/uploads/posts/" + fileName;
            }
            else
            {
                //var existingPost = _postService.GetPostById(post.Id);
                post.ImagePath = existingPost?.ImagePath;
            }

            if (_postService.UpdatePost(post))
                return RedirectToAction("ViewProfile", "Profiles", new { id = currentUserId });

            ModelState.AddModelError("", "Failed to update post.");
            return View(post);
        }


        [HttpPost]
        public IActionResult Delete(int id)
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (!_postService.DeletePost(id, currentUserId))
            {
                return BadRequest("Cannot delete this post.");
            }

            return RedirectToAction("ViewProfile", "Profiles", new { id = currentUserId });
        }

        [HttpPost]
        public IActionResult Like(int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _postService.ToggleLike(postId, userId);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ToggleLike(int postId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _postService.ToggleLike(userId, postId);
            int updatedCount = _postService.GetLikeCount(postId);

            return Json(new { success = true, likeCount = updatedCount });

           
        }


        //////////
        ///Comments 
        ///
        [HttpGet]
        public IActionResult GetComments(int postId)
        {
            var comments = _commentService.GetCommentsByPost(postId);
            return PartialView("_CommentsList", comments);
        }

        [HttpPost]
        public IActionResult AddComment(int postId, string text, int? parentCommentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            _commentService.AddComment(new Comment
            {
                PostId = postId,
                UserId = userId,
                Text = text,
                ParentCommentId = parentCommentId
            });

            var updatedComments = _commentService.GetCommentsByPost(postId);
            return PartialView("_CommentsList", updatedComments); // partial for AJAX
        }


        [HttpPost]
        public IActionResult DeleteComment(int commentId, int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Fetch the comment first
            var comment = _commentService.GetCommentsByPost(postId)
                                         .SelectMany(c => FlattenComments(c))
                                         .FirstOrDefault(c => c.Id == commentId);

            if (comment == null)
                return NotFound();

            if (comment.UserId != userId)
                return Forbid();

            _commentService.DeleteComment(commentId);

            var updatedComments = _commentService.GetCommentsByPost(postId);
            return PartialView("_CommentsList", updatedComments);
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
