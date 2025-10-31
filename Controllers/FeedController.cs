using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniSocial.Dtos;
using MiniSocial.Services;

namespace MiniSocial.Controllers
{
    public class FeedController : Controller
    {
        private readonly FeedService _feedService;
        private readonly PostService _postService;
        private readonly CommentService _commentService;

        public FeedController(FeedService feedService, PostService postService, CommentService commentService)
        {
            _feedService = feedService;
            _postService = postService;
            _commentService = commentService;
        }


        // GET: /Feed/Following?offset=0&limit=10
        [HttpGet]
        [Authorize]
        public IActionResult Following(int offset = 0, int limit = 10)
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var posts = _feedService.GetFollowingFeed(currentUserId, offset, limit);

            //infinite scroll
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PostListPartial", posts);
            }

            // Normal first page load
            return View("FeedPage", posts);

            
            //return View(posts);
        }


        // GET: /Feed/ForYou?offset=0&limit=10
        [HttpGet]
        public IActionResult ForYou(int offset = 0, int limit = 10)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var posts = _feedService.GetForYouFeed(currentUserId, offset, limit);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PostListPartial", posts);
            }

            return View("FeedPage", posts);
        }

        public IActionResult Post(int id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
                return NotFound();

            var comments = _commentService.GetCommentsByPost(id);

            var viewModel = new PostDetailsDto
            {
                Post = post,
                Comments = comments
            };
                
            return View("Comments", viewModel);
        }







    }
}
