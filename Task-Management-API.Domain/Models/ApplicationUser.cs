using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Task_Management_API.Domain.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Country { get; set; }
        public string? ImagePath { get; set; }
        public ICollection<AppTask>? Tasks { get; set; }
        [JsonIgnore]
        public ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
