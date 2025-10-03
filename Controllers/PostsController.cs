using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MiniSocial.Models;
using MiniSocial.Services;

namespace MiniSocial.Controllers
{
    public class PostsController : Controller
    {
        private readonly PostService _postService;

        public PostsController(PostService postService)
        {
            _postService = postService;
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
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult UserPosts(int UserId)
        {
            var posts = _postService.GetPosts(UserId);
            return View(posts);
        }
    }
}
