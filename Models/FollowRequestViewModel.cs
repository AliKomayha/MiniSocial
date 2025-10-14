namespace MiniSocial.Models
{
    public class FollowRequestViewModel
    {
        public int FollowId { get; set; }
        public int FollowerId { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
    }
}
