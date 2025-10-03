using MiniSocial.Dtos;

namespace MiniSocial.Models
{
    public class ProfileViewModel
    {
        public Profile Profile { get; set; }
        public ProfileDto Counts { get; set; }

        
        public List<PostDto> Posts { get; set; }

    }
}
