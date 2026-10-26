using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BistroGo.Core.Models
{
    /// <summary>
    /// A restaurant review submitted by a customer.
    /// Can exist independently from store order records.
    /// </summary>
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int Id { get; set; }

        // FK -> AspNetUsers.Id, wired up in the DbContext
        [MaxLength(450)]
        public string? CustomerId { get; set; }

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? ReviewText { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsArchived { get; set; } = false;

        public List<ReviewComment> Comments { get; set; } = new();

        /// <summary>
        /// Converts the UTC-stored CreatedAt timestamp to local time for user-facing display.
        /// Handles DateTimeKind.Utc, DateTimeKind.Local, and DateTimeKind.Unspecified (from SQLite).
        /// </summary>
        [NotMapped]
        public DateTime LocalCreatedAt => CreatedAt.Kind == DateTimeKind.Local
            ? CreatedAt
            : DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();

        // Convenience / compatibility aliases
        [NotMapped]
        public string? ReviewerName
        {
            get => CustomerName;
            set => CustomerName = value;
        }

        [NotMapped]
        public string? UserId
        {
            get => CustomerId;
            set => CustomerId = value;
        }

        [NotMapped]
        public int Stars
        {
            get => Rating;
            set => Rating = value;
        }

        [NotMapped]
        public string? Comment
        {
            get => ReviewText;
            set => ReviewText = value;
        }

        [NotMapped]
        public string? Text
        {
            get => ReviewText;
            set => ReviewText = value;
        }

        [NotMapped]
        public DateTime SubmittedAt
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }
    }
}
