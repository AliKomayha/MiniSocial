namespace MiniSocial.Dtos
{
    public class PostDetailsDto
    {
        public PostDto Post { get; set; }
        public List<CommentDto> Comments { get; set; }
    }
}
