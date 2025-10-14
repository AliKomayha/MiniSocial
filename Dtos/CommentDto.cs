using MiniSocial.Models;

namespace MiniSocial.Dtos
{
    public class CommentDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string? Avatar { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentCommentId { get; set; }
        public List<CommentDto> Replies { get; set; } = new();

    }
}
