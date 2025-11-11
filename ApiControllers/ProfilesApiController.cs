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
    public class ProfilesApiController : ControllerBase
    {

        private readonly string _connectionString;
        private readonly IConfiguration _config;
        private readonly FollowService _followService;
        private readonly ProfileService _profileService;
        private readonly PostService _postService;
        private readonly CommentService _commentService;

        public ProfilesApiController(IConfiguration config, FollowService followService, ProfileService profileService, PostService postService, CommentService commentService)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _config = config;
            _followService = followService;
            _profileService = profileService;
            _postService = postService;
            _commentService = commentService;
        }


        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetProfile(int id)
        {
            var profile = _profileService.GetProfileByUserId(id);
            if (profile == null)
                return NotFound(new { message = "Profile not found" });

            //var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var currentUserId = int.Parse(User.FindFirstValue("userId"));
            var isApprovedFollower = _followService.IsApprovedFollower(currentUserId, profile.UserId);
            var followStatus = _followService.GetFollowStatus(currentUserId, profile.UserId);

            var posts = _postService.GetPosts(id);
            foreach (var post in posts)
            {
                post.Comments = _commentService.GetCommentsByPost(post.Id);
            }

            var counts = new ProfileDto
            {
                FollowersCount = _followService.GetFollowers(profile.UserId).Count,
                FollowingCount = _followService.GetFollowing(profile.UserId).Count
            };

            var viewModel = new ProfileViewModel
            {
                Profile = profile,
                Counts = counts,
                Posts = posts,
                IsPrivate = profile.IsPrivate,
                IsApprovedFollower = isApprovedFollower,
                FollowStatus = followStatus


            };

            // === Privacy logic (same as MVC) ===
            if (!profile.IsPrivate || currentUserId == profile.UserId || isApprovedFollower)
                return Ok(viewModel);  // Full profile data

            else
                return Ok(new
                {
                    profile.Id,
                    profile.DisplayName,
                    profile.Avatar,
                    profile.Bio,
                    profile.IsPrivate,
                    viewModel.FollowStatus,
                    message = "This account is private"
                }); //limited profile

        }



        [Authorize]
        [HttpGet ("{id}")]
        public IActionResult FollowRequests()
        {
            var currentUserId = int.Parse(User.FindFirstValue("userId"));

            var pendingRequests = _followService.GetPendingRequests(currentUserId);

            var requests = new List<dynamic>();

            /// continue
            /// 
            ///
            ///
            ///
            ///
            ///
            ///
            ///
            
            return Ok();
        }


        // follow unfollow  feature 
        [Authorize]
        [HttpPost ("follow/{followinngId}")]
        public IActionResult Follow(int followingId)
        {
            var followerId = int.Parse(User.FindFirstValue("userId"));
            var status = _followService.RequestFollow(followerId, followingId);
            return Ok(new { success = true, status });
        }

        [Authorize]
        [HttpPost("unfollow/{followinngId}")]
        public IActionResult UnFollow(int followingId)
        {
            var followerId = int.Parse(User.FindFirstValue("userId"));
            _followService.UnFollow(followerId, followingId);
            return Ok(new { success = true });

        }
        [Authorize]
        [HttpPost("accept/{followId}")]
        public IActionResult AcceptRequest(int followId)
        {
            _followService.ApproveFollow(followId);
            return Ok(new { success = true });
        }

        [Authorize]
        [HttpPost("decline/{followId}")]
        public IActionResult DeclineRequest(int followId)
        {
            _followService.RejectFollow(followId);
            return Ok(new { success = true });
        }



        /// get followers and following  list 
        [Authorize]
        [HttpGet("{userId}")]
        public IActionResult Followers(int userId)
        {
            var profile = _profileService.GetProfileByUserId(userId);
            if (profile == null) return NotFound();

            var followers = _followService.GetFollowers(userId);
            var counts = new ProfileDto
            {
                FollowersCount = followers.Count,
                FollowingCount = _followService.GetFollowing(userId).Count
            };

            return Ok(new { Profile = profile, Counts = counts, Followers = followers });
        }

        [Authorize]
        [HttpGet("{userId}")]
        public IActionResult Following(int userId)
        {
            var profile = _profileService.GetProfileByUserId(userId);
            if (profile == null) return NotFound();

            var following = _followService.GetFollowing(userId);
            var counts = new ProfileDto
            {
                FollowersCount = _followService.GetFollowers(userId).Count,
                FollowingCount = following.Count
            };

            return Ok(new { Profile = profile, Counts = counts, Following = following });
        }

        /// search profile /
        [Authorize]
        [HttpGet("search")]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<SearchProfileDto>());

            var results = _profileService.SearchProfiles(query);
            return Ok(results);
        }


            



    }
}
