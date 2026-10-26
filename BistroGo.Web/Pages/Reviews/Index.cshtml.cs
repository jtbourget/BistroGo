using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BistroGo.Web.Pages.Reviews
{
    /// <summary>
    /// PageModel for customer reviews and manager comments.
    /// Accessible to customers, guests, and managers. Kitchen staff are restricted.
    /// Customers and guests can submit reviews and view feedback.
    /// Managers can view feedback and post official manager comments, but cannot submit reviews.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(IReviewRepository reviewRepo, UserManager<ApplicationUser> userManager)
        {
            _reviewRepo = reviewRepo;
            _userManager = userManager;
        }

        public string? CurrentUserName { get; set; }
        public bool IsManager { get; set; }
        public bool CanSubmitReview { get; set; }

        public List<Review> Reviews { get; set; } = new();
        public double AverageRating => Reviews.Count > 0 ? Reviews.Average(r => r.Rating) : 0;

        [BindProperty]
        public ReviewInputModel ReviewInput { get; set; } = new();

        [BindProperty]
        public CommentInputModel CommentInput { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class ReviewInputModel
        {
            [Required(ErrorMessage = "Please select a star rating.")]
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
            public int Rating { get; set; } = 5;

            [MaxLength(100)]
            public string? ReviewerName { get; set; }

            [Required(ErrorMessage = "Please provide written feedback.")]
            [MaxLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters.")]
            public string ReviewComment { get; set; } = string.Empty;
        }

        public class CommentInputModel
        {
            public int ReviewId { get; set; }

            [Required(ErrorMessage = "Comment cannot be empty.")]
            [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
            public string CommentText { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Kitchen staff are not permitted to view or submit reviews
            if (User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.KitchenStaff))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            IsManager = User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.Manager);
            CanSubmitReview = !IsManager;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !string.IsNullOrWhiteSpace(user.FirstName))
                {
                    CurrentUserName = $"{user.FirstName} {user.LastName}".Trim();
                }
                else
                {
                    CurrentUserName = User.Identity.Name;
                }
            }

            Reviews = await _reviewRepo.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSubmitReviewAsync()
        {
            // Kitchen staff are not permitted to submit reviews
            if (User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.KitchenStaff))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // Managers are not permitted to submit customer reviews
            if (User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.Manager))
            {
                ErrorMessage = "Managers are not permitted to submit reviews.";
                return RedirectToPage();
            }

            if (ReviewInput.Rating < 1 || ReviewInput.Rating > 5 || string.IsNullOrWhiteSpace(ReviewInput.ReviewComment))
            {
                ErrorMessage = "Please provide both a star rating and written feedback.";
                return RedirectToPage();
            }

            string? customerId = null;
            string reviewerName = ReviewInput.ReviewerName?.Trim() ?? string.Empty;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                customerId = user?.Id;
                if (string.IsNullOrWhiteSpace(reviewerName))
                {
                    if (user != null && !string.IsNullOrWhiteSpace(user.FirstName))
                    {
                        reviewerName = $"{user.FirstName} {user.LastName}".Trim();
                    }
                    else
                    {
                        reviewerName = User.Identity.Name ?? "Customer";
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(reviewerName))
                {
                    reviewerName = "Guest";
                }
            }

            var review = new Review
            {
                CustomerId = customerId,
                CustomerName = reviewerName,
                Rating = ReviewInput.Rating,
                ReviewText = ReviewInput.ReviewComment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepo.AddAsync(review);
            SuccessMessage = "Thank you! Your review has been submitted successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddCommentAsync()
        {
            // Only managers can comment on reviews
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole(Roles.Manager))
            {
                return Forbid();
            }

            if (CommentInput.ReviewId <= 0 || string.IsNullOrWhiteSpace(CommentInput.CommentText))
            {
                ErrorMessage = "Please enter comment text before submitting.";
                return RedirectToPage();
            }

            var review = await _reviewRepo.GetByIdAsync(CommentInput.ReviewId);
            if (review == null)
            {
                ErrorMessage = "Review not found.";
                return RedirectToPage();
            }

            var manager = await _userManager.GetUserAsync(User);
            string managerName = "Store Manager";
            if (manager != null && !string.IsNullOrWhiteSpace(manager.FirstName))
            {
                managerName = $"{manager.FirstName} {manager.LastName}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
            {
                managerName = User.Identity.Name;
            }

            var comment = new ReviewComment
            {
                ReviewId = CommentInput.ReviewId,
                ManagerId = manager?.Id,
                ManagerName = managerName,
                CommentText = CommentInput.CommentText.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepo.AddCommentAsync(comment);
            SuccessMessage = "Your manager response has been posted.";
            return RedirectToPage(null, null, $"review-{CommentInput.ReviewId}");
        }
    }
}
