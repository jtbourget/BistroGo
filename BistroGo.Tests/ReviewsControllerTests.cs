using BistroGo.Api.Controllers;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace BistroGo.Tests;

public class ReviewsControllerTests
{
    private readonly IReviewRepository _repo = Substitute.For<IReviewRepository>();
    private readonly ReviewsController _controller;

    public ReviewsControllerTests()
    {
        _controller = new ReviewsController(_repo);
    }

    [Fact]
    public async Task Get_ReturnsAllReviews()
    {
        var reviews = new List<Review>
        {
            new Review { Id = 1, Rating = 5, ReviewText = "Superb food!", CustomerName = "Alice" },
            new Review { Id = 2, Rating = 4, ReviewText = "Great service", CustomerName = "Bob" }
        };
        _repo.GetAllAsync().Returns(reviews);

        var result = await _controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnReviews = Assert.IsAssignableFrom<IEnumerable<Review>>(okResult.Value);
        Assert.Equal(2, returnReviews.Count());
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var review = new Review { Id = 1, Rating = 5, ReviewText = "Delicious" };
        _repo.GetByIdAsync(1).Returns(review);

        var result = await _controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Review>(okResult.Value);
        Assert.Equal(1, returned.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(999).Returns((Review?)null);

        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Post_CreatesReview_ReturnsCreatedAtAction()
    {
        var newReview = new Review { Rating = 5, ReviewText = "Loved it!", CustomerName = "Charlie" };
        _repo.AddAsync(Arg.Any<Review>()).Returns(callInfo =>
        {
            var r = callInfo.Arg<Review>();
            r.Id = 42;
            return r;
        });

        var result = await _controller.Post(newReview);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var review = Assert.IsType<Review>(created.Value);
        Assert.Equal(42, review.Id);
    }

    [Fact]
    public async Task PostComment_WhenReviewNotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(99).Returns((Review?)null);

        var comment = new ReviewComment { CommentText = "Thank you!" };
        var result = await _controller.PostComment(99, comment);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostComment_WhenReviewExists_AddsCommentAndReturnsOk()
    {
        var review = new Review { Id = 10, Rating = 4, ReviewText = "Nice!" };
        _repo.GetByIdAsync(10).Returns(review);
        _repo.AddCommentAsync(Arg.Any<ReviewComment>()).Returns(callInfo =>
        {
            var c = callInfo.Arg<ReviewComment>();
            c.Id = 1;
            return c;
        });

        var comment = new ReviewComment
        {
            ManagerId = "manager-1",
            ManagerName = "Store Manager",
            CommentText = "Thank you for the review!"
        };

        var result = await _controller.PostComment(10, comment);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var createdComment = Assert.IsType<ReviewComment>(okResult.Value);
        Assert.Equal(10, createdComment.ReviewId);
        Assert.Equal("Store Manager", createdComment.ManagerName);
        Assert.Equal(1, createdComment.Id);
    }

    [Fact]
    public async Task GetComments_ReturnsAllCommentsForReview()
    {
        var review = new Review { Id = 10 };
        _repo.GetByIdAsync(10).Returns(review);
        var comments = new List<ReviewComment>
        {
            new ReviewComment { Id = 1, ReviewId = 10, ManagerName = "Store Manager", CommentText = "Response 1" },
            new ReviewComment { Id = 2, ReviewId = 10, ManagerName = "Store Manager", CommentText = "Response 2" }
        };
        _repo.GetCommentsByReviewIdAsync(10).Returns(comments);

        var result = await _controller.GetComments(10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnComments = Assert.IsAssignableFrom<IEnumerable<ReviewComment>>(okResult.Value);
        Assert.Equal(2, returnComments.Count());
    }
}
