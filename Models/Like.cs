using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniSocial.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int PostId { get; set; }

        [ValidateNever]
        public User User { get; set; }
        [ValidateNever]
        public Post Post { get; set; }
    }
}
