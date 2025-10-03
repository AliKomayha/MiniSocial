using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniSocial.Models
{
    public class Profile
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
       
        [Required, MaxLength(100)]
        public string DisplayName { get; set; }

        public string? Avatar { get; set; }
        public string? Bio { get; set; }

        public DateTime? BirthDate { get; set; }

        public bool IsPrivate { get; set; } = false;
        

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public User User { get; set; }

       
        [NotMapped] // prevents DB errors
        public IFormFile? AvatarFile { get; set; }
    }
}


