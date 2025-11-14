using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniSocial.Dtos;
using MiniSocial.Services;

namespace MiniSocial.ApiControllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FeedApiController : ControllerBase
    {
        private readonly FeedService _feedService;

        public FeedApiController(FeedService feedService)
        {
            _feedService = feedService;
        }

        // GET: api/feed/following?offset=0&limit=10
        [HttpGet]
        public ActionResult<List<PostDto>> GetFollowingFeed([FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            int currentUserId = int.Parse(User.FindFirstValue("userId"));
            var posts = _feedService.GetFollowingFeed(currentUserId, offset, limit);

            return Ok(posts);
        }

    }
}
