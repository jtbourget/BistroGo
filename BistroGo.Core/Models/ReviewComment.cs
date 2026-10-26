using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    /// <summary>
    /// A comment or response left on a customer review, typically by a store manager.
    /// </summary>
    [Table("ReviewComments")]
    public class ReviewComment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Review))]
        public int ReviewId { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        [JsonIgnore]
        public Review? Review { get; set; }

        // FK -> AspNetUsers.Id (Manager who posted this comment)
        [MaxLength(450)]
        public string? ManagerId { get; set; }

        [MaxLength(100)]
        public string? ManagerName { get; set; }

        [Required, MaxLength(1000)]
        public string CommentText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Converts the UTC-stored CreatedAt timestamp to local time for user-facing display.
        /// Handles DateTimeKind.Utc, DateTimeKind.Local, and DateTimeKind.Unspecified (from SQLite).
        /// </summary>
        [NotMapped]
        public DateTime LocalCreatedAt => CreatedAt.Kind == DateTimeKind.Local
            ? CreatedAt
            : DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();
    }
}
