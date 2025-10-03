namespace MiniSocial.Dtos
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostsCount { get; set; }
    }
}
