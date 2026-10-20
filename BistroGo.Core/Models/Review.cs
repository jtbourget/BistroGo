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

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? ReviewText { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsArchived { get; set; } = false;

        // Convenience / compatibility aliases
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
