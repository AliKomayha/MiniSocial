using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MiniSocial.Models;
using MiniSocial.Dtos;
using MiniSocial.Services;
using Microsoft.AspNetCore.Authorization;

namespace MiniSocial.Controllers
{
    public class ProfilesController : Controller
    {

        private readonly string _connectionString;
        private readonly FollowService _followService;
        private readonly ProfileService _profileService;
        private readonly PostService _postService;
        public ProfilesController(IConfiguration config, FollowService followService, ProfileService profileService, PostService postService)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _followService = followService;
            _profileService = profileService;
            _postService = postService;

        }



        [Authorize]
        public IActionResult ViewProfile(int id)
        {
            var profile = GetProfileById(id);
            if (profile == null)
                return NotFound();

            var counts = new ProfileDto();
            counts = GetFollowersCount(profile.UserId);

            var posts = _postService.GetPosts(id);


            var viewModel = new ProfileViewModel
            {
                Profile = profile,
                Counts = counts,
                Posts = posts
            };

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Privacy logic
            if (!profile.IsPrivate)
            {
                return View("FullProfile", viewModel);
            }
            else
            {
                if (currentUserId == profile.UserId)
                {
                    return View("FullProfile", viewModel); // owner
                }
                else if (_followService.IsApprovedFollower(currentUserId, profile.UserId))
                {
                    return View("FullProfile", viewModel); // follower
                }
                else
                {
                    return View("LimitedProfile", viewModel); // restricted view
                }

            }
        }


        private ProfileDto GetFollowersCount(int userId)
        {
            var counts = new ProfileDto();

            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM Follows WHERE FollowingId=@UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                counts.FollowersCount = (int)command.ExecuteScalar();

                command = new SqlCommand(
                    "SELECT COUNT(*) FROM Follows WHERE FollowerId=@UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                counts.FollowingCount = (int)command.ExecuteScalar();
            }
            return counts;

        }

        [HttpGet]

        public IActionResult EditProfile()
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var profile = _profileService.GetProfileByUserId(currentUserId);

            if(profile == null)
                return NotFound();
            
            return View(profile);
        }

        [HttpPost]
        public IActionResult EditProfile(Profile profile)
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            profile.UserId = currentUserId; // enforce current user

            // Handle Avatar upload
            if (profile.AvatarFile != null && profile.AvatarFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(profile.AvatarFile.FileName)}";
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    profile.AvatarFile.CopyTo(stream);
                }

                profile.Avatar = $"/uploads/{fileName}";
            }


            bool success = _profileService.UpdateProfile(profile);

            if (success)
                return RedirectToAction("ViewProfile", new { id = currentUserId });

            ModelState.AddModelError("", "Failed to update profile.");
            return View(profile);
        }

        private Profile? GetProfileById(int id)
        {
            Profile? profile = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT Profiles.Id, Profiles.DisplayName, Avatar, Bio, BirthDate, IsPrivate, Users.Id, Users.UserName FROM Profiles JOIN Users ON Users.Id = Profiles.UserID WHERE Profiles.Id=@Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    profile = new Profile
                    {
                        Id = reader.GetInt32(0),
                        DisplayName = reader.GetString(1),
                        Avatar = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Bio = reader.IsDBNull(3) ? null : reader.GetString(3),
                        BirthDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        IsPrivate = reader.GetBoolean(5),
                        UserId = reader.GetInt32(6),
                        User = new User
                        {
                            Id = reader.GetInt32(6),
                            Username = reader.GetString(7)
                        }
                    };
                }
            }
            return profile;
        }
    }
}
