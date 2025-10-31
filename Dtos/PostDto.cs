using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MiniSocial.Models;

namespace MiniSocial.Dtos
{
    public class PostDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } // from User
        public string DisplayName { get; set; } //from profile
        public string? Avatar { get; set; } // from User
        public string Text { get; set; }
        public string? ImagePath { get; set; }

        public bool IsPrivate { get; set; } // from profile
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }

        public DateTime CreatedAt { get; set; }

        [ValidateNever]
        public User User { get; set; }

        public List<CommentDto> Comments { get; set; } = new();

       
    }
}
