using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniSocial.Models
{
    public class Follow
    {
        public int Id { get; set; }
        public int FollowerId { get; set; }
        public int FollowingId { get; set; }
 
        public string Status { get; set; } = "Pending"; // Possible values: "Pending", "Accepted", "Rejected"

        [ValidateNever]
        public User Follower { get; set; }

        [ValidateNever]
        public User Following { get; set; }

    }
}
