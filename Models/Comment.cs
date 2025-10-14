using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniSocial.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int PostId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required, MaxLength(300)]
        public string Text { get; set; }

        public int? ParentCommentId { get; set; }

        [ValidateNever]
        public User User { get; set; }
        [ValidateNever]
        public Post Post { get; set; }
    }
}
