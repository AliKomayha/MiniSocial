using MiniSocial.Dtos;

namespace MiniSocial.Models
{
    public class ProfileViewModel
    {
        public Profile Profile { get; set; }
        public ProfileDto Counts { get; set; }

        public List<PostDto> Posts { get; set; }

        public bool IsPrivate { get; set; }
        public bool IsApprovedFollower { get; set; }
        public string FollowStatus { get; set; }

       ////
        public List<Profile> Followers { get; set; } = new();
        public List<Profile> Following { get; set; } = new();
    }
}
