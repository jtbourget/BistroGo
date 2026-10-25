using System.Security.Claims;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Identity;
using BistroGo.Web.Pages.Reviews;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace BistroGo.Tests;

public class ReviewsPageModelTests
{
    private readonly IReviewRepository _repo = Substitute.For<IReviewRepository>();
    private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsPageModelTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            _userStore, null, null, null, null, null, null, null, null);
    }

    private IndexModel CreateModel(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        return new IndexModel(_repo, _userManager)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnGet_WhenKitchenStaff_RedirectsToAccessDenied()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "CookBob"),
            new Claim(ClaimTypes.Role, Roles.KitchenStaff)
        }, "TestAuth"));

        var model = CreateModel(user);

        var result = await model.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/AccessDenied", redirect.PageName);
    }

    [Fact]
    public async Task OnGet_WhenCustomer_AllowsAccessAndLoadsReviews()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Alice"),
            new Claim(ClaimTypes.Role, Roles.Customer)
        }, "TestAuth"));

        _repo.GetAllAsync().Returns(new List<Review>
        {
            new Review { Id = 1, Rating = 5, ReviewText = "Loved the pasta!" }
        });

        var model = CreateModel(user);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.IsManager);
        Assert.True(model.CanSubmitReview);
        Assert.Single(model.Reviews);
    }

    [Fact]
    public async Task OnGet_WhenManager_AllowsAccessAndIdentifiesAsManager()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Boss"),
            new Claim(ClaimTypes.Role, Roles.Manager)
        }, "TestAuth"));

        _repo.GetAllAsync().Returns(new List<Review>());

        var model = CreateModel(user);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.IsManager);
        Assert.False(model.CanSubmitReview); // Managers shouldn't be able to fill out reviews
    }

    [Fact]
    public async Task OnGet_WhenGuest_AllowsAccessAndCanSubmitReview()
    {
        var guestUser = new ClaimsPrincipal(new ClaimsIdentity()); // unauthenticated

        _repo.GetAllAsync().Returns(new List<Review>());

        var model = CreateModel(guestUser);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.IsManager);
        Assert.True(model.CanSubmitReview);
    }

    [Fact]
    public async Task OnPostSubmitReview_WhenKitchenStaff_RedirectsToAccessDenied()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "CookBob"),
            new Claim(ClaimTypes.Role, Roles.KitchenStaff)
        }, "TestAuth"));

        var model = CreateModel(user);
        model.ReviewInput = new IndexModel.ReviewInputModel
        {
            Rating = 5,
            ReviewComment = "Sneaking a review"
        };

        var result = await model.OnPostSubmitReviewAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/AccessDenied", redirect.PageName);
        await _repo.DidNotReceive().AddAsync(Arg.Any<Review>());
    }

    [Fact]
    public async Task OnPostSubmitReview_WhenManager_RejectsSubmission()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "StoreManager"),
            new Claim(ClaimTypes.Role, Roles.Manager)
        }, "TestAuth"));

        var model = CreateModel(user);
        model.ReviewInput = new IndexModel.ReviewInputModel
        {
            Rating = 5,
            ReviewComment = "I am the manager and love this place"
        };

        var result = await model.OnPostSubmitReviewAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Managers are not permitted", model.ErrorMessage);
        await _repo.DidNotReceive().AddAsync(Arg.Any<Review>());
    }

    [Fact]
    public async Task OnPostSubmitReview_WhenCustomer_SuccessfullyAddsReview()
    {
        var appUser = new ApplicationUser { Id = "user-123", FirstName = "Alice", LastName = "Smith" };
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "alice@test.com"),
            new Claim(ClaimTypes.Role, Roles.Customer)
        }, "TestAuth"));

        _userManager.GetUserAsync(user).Returns(appUser);

        var model = CreateModel(user);
        model.ReviewInput = new IndexModel.ReviewInputModel
        {
            Rating = 4,
            ReviewerName = "Alice Smith",
            ReviewComment = "Great dining experience!"
        };

        var result = await model.OnPostSubmitReviewAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(model.SuccessMessage);
        await _repo.Received(1).AddAsync(Arg.Is<Review>(r =>
            r.CustomerId == "user-123" &&
            r.CustomerName == "Alice Smith" &&
            r.Rating == 4 &&
            r.ReviewText == "Great dining experience!"));
    }

    [Fact]
    public async Task OnPostSubmitReview_WhenGuest_SuccessfullyAddsReviewWithGuestName()
    {
        var guestUser = new ClaimsPrincipal(new ClaimsIdentity());

        var model = CreateModel(guestUser);
        model.ReviewInput = new IndexModel.ReviewInputModel
        {
            Rating = 5,
            ReviewerName = "", // empty, should default to Guest
            ReviewComment = "Best burger in town!"
        };

        var result = await model.OnPostSubmitReviewAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(model.SuccessMessage);
        await _repo.Received(1).AddAsync(Arg.Is<Review>(r =>
            r.CustomerId == null &&
            r.CustomerName == "Guest" &&
            r.Rating == 5 &&
            r.ReviewText == "Best burger in town!"));
    }

    [Fact]
    public async Task OnPostAddComment_WhenManager_SuccessfullyAddsCommentSignifiedAsManager()
    {
        var managerUser = new ApplicationUser { Id = "mgr-1", FirstName = "Jane", LastName = "Doe" };
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mgr-1"),
            new Claim(ClaimTypes.Name, "manager@bistrogo.com"),
            new Claim(ClaimTypes.Role, Roles.Manager)
        }, "TestAuth"));

        _userManager.GetUserAsync(user).Returns(managerUser);
        _repo.GetByIdAsync(10).Returns(new Review { Id = 10, Rating = 3, ReviewText = "Service was a bit slow." });

        var model = CreateModel(user);
        model.CommentInput = new IndexModel.CommentInputModel
        {
            ReviewId = 10,
            CommentText = "We apologize for the delay and are working on improving our kitchen times."
        };

        var result = await model.OnPostAddCommentAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(model.SuccessMessage);
        await _repo.Received(1).AddCommentAsync(Arg.Is<ReviewComment>(c =>
            c.ReviewId == 10 &&
            c.ManagerId == "mgr-1" &&
            c.ManagerName == "Jane Doe" &&
            c.CommentText.Contains("We apologize")));
    }

    [Fact]
    public async Task OnPostAddComment_WhenNotManager_ReturnsForbid()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "CustomerSam"),
            new Claim(ClaimTypes.Role, Roles.Customer)
        }, "TestAuth"));

        var model = CreateModel(user);
        model.CommentInput = new IndexModel.CommentInputModel
        {
            ReviewId = 10,
            CommentText = "I agree!"
        };

        var result = await model.OnPostAddCommentAsync();

        Assert.IsType<ForbidResult>(result);
        await _repo.DidNotReceive().AddCommentAsync(Arg.Any<ReviewComment>());
    }
}
