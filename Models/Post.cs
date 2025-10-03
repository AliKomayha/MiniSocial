using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniSocial.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(500)]
        public string Text { get; set; }


        public string? ImagePath { get; set; }

        [ValidateNever]
        public User User { get; set; }
     

    }
}
