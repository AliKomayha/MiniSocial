using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniSocial.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; }

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; }

        public bool EmailConfirmed { get; set; } = false;

        [Required]
        public string PasswordHash { get; set; }
        [Required]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

        public string? PasswordResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public Profile Profile { get; set; }

    }
}
