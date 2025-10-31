using MiniSocial.Dtos;
using MiniSocial.Repositories;

namespace MiniSocial.Services
{
    public class FeedService
    {

        private readonly FeedRepository _feedRepository;

        public FeedService(FeedRepository feedRepository)
        {
            _feedRepository = feedRepository;
        }

        public List<PostDto> GetFollowingFeed(int currentUserId, int offset, int limit)
        {
            return _feedRepository.GetFeedPosts(currentUserId, offset, limit);
        }

        public List<PostDto> GetForYouFeed(int currentUserId, int offset, int limit)
        {
            // Later you can apply ranking logic here:
            // e.g., trending posts, popular users, recommended topics

            var posts = _feedRepository.GetFeedPosts(currentUserId, offset, limit);

            // Example placeholder for future improvement:
            // posts = posts.OrderByDescending(p => p.LikeCount).ToList();

            return posts;
        }
    }
}
