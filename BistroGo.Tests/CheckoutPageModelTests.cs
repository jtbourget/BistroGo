using System.Security.Claims;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Identity;
using BistroGo.Web.Cart;
using BistroGo.Web.Pages.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using Xunit;

namespace BistroGo.Tests;

public class CheckoutPageModelTests
{
    private readonly ICartService _cart = Substitute.For<ICartService>();
    private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutPageModelTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            _userStore, null, null, null, null, null, null, null, null);
    }

    private Order CreateMockCart(int itemCount = 2)
    {
        var order = new Order
        {
            Id = 1,
            Subtotal = 20.00m,
            Tax = 1.00m,
            Total = 21.00m
        };

        for (int i = 1; i <= itemCount; i++)
        {
            order.Items.Add(new OrderItem
            {
                Id = i,
                MenuItemId = i,
                Quantity = 1,
                UnitPrice = 10.00m,
                MenuItem = new MenuItem { Id = i, Name = $"Item {i}", Price = 10.00m }
            });
        }

        return order;
    }

    private CheckoutModel CreateModel(ClaimsPrincipal user, Order? cart = null, Guid? guestToken = null)
    {
        var httpContext = new DefaultHttpContext { User = user };

        if (guestToken.HasValue)
        {
            httpContext.Request.Headers["Cookie"] = $"BistroGo.GuestToken={guestToken.Value}";
        }

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(cart);

        return new CheckoutModel(_cart, _userManager)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnGet_WhenUserIsLoggedIn_PreFillsNameEmailAndPhone()
    {
        var userId = "user-123";
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "AliceJohnson")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            PhoneNumber = "555-0101"
        };

        _userManager.GetUserAsync(userPrincipal).Returns(appUser);

        var model = CreateModel(userPrincipal, CreateMockCart());

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Alice Johnson", model.GuestName);
        Assert.Equal("alice@example.com", model.GuestEmail);
        Assert.Equal("555-0101", model.GuestPhone);
    }

    [Fact]
    public async Task OnGet_WhenUserIsLoggedIn_PhoneNotSaved_PhoneIsNull()
    {
        var userId = "user-123";
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "AliceJohnson")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            PhoneNumber = null
        };

        _userManager.GetUserAsync(userPrincipal).Returns(appUser);

        var model = CreateModel(userPrincipal, CreateMockCart());

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Alice Johnson", model.GuestName);
        Assert.Equal("alice@example.com", model.GuestEmail);
        Assert.Null(model.GuestPhone);
    }

    [Fact]
    public async Task OnGet_WhenUserHasOnlyFirstName_PreFillsFirstNameWithoutTrailingSpace()
    {
        var userId = "user-123";
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "Alice",
            LastName = "",
            Email = "alice@example.com"
        };

        _userManager.GetUserAsync(userPrincipal).Returns(appUser);

        var model = CreateModel(userPrincipal, CreateMockCart());

        await model.OnGetAsync();

        Assert.Equal("Alice", model.GuestName);
    }

    [Fact]
    public async Task OnGet_WhenUserHasOnlyLastName_PreFillsLastNameWithoutLeadingSpace()
    {
        var userId = "user-123";
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "",
            LastName = "Johnson",
            Email = "johnson@example.com"
        };

        _userManager.GetUserAsync(userPrincipal).Returns(appUser);

        var model = CreateModel(userPrincipal, CreateMockCart());

        await model.OnGetAsync();

        Assert.Equal("Johnson", model.GuestName);
    }

    [Fact]
    public async Task OnGet_WhenUserIsGuest_FieldsAreEmpty()
    {
        // Unauthenticated user
        var guestPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var guestToken = Guid.NewGuid();

        var model = CreateModel(guestPrincipal, CreateMockCart(), guestToken);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(model.GuestName);
        Assert.Null(model.GuestEmail);
        Assert.Null(model.GuestPhone);
        Assert.Null(model.SpecialInstructions);
    }

    [Fact]
    public async Task OnGet_WhenCartIsEmpty_RedirectsToMenu()
    {
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
        }, "TestAuth"));

        var emptyCart = new Order { Items = new List<OrderItem>() };
        var model = CreateModel(userPrincipal, emptyCart);

        var result = await model.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Menu/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPost_WhenValidationFails_TypedValuesStay_NotReplacedWithProfileValues()
    {
        var userId = "user-123";
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "AliceJohnson")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            PhoneNumber = "555-0101"
        };
        _userManager.GetUserAsync(userPrincipal).Returns(appUser);

        var model = CreateModel(userPrincipal, CreateMockCart());

        // Simulate user clearing the name and editing other fields
        model.GuestName = ""; // Invalid: Name is required
        model.GuestEmail = "custom-typed@example.com";
        model.GuestPhone = "999-0000";
        model.SpecialInstructions = "Extra napkins please";

        var result = await model.OnPostAsync();

        var pageResult = Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(nameof(CheckoutModel.GuestName)));
        Assert.NotEmpty(model.ModelState[nameof(CheckoutModel.GuestName)]!.Errors);

        // Verify typed values stay and were NOT replaced with profile values
        Assert.Equal("", model.GuestName);
        Assert.Equal("custom-typed@example.com", model.GuestEmail);
        Assert.Equal("999-0000", model.GuestPhone);
        Assert.Equal("Extra napkins please", model.SpecialInstructions);

        // Ensure GetUserAsync was not even called during OnPost
        await _userManager.DidNotReceiveWithAnyArgs().GetUserAsync(default!);
    }

    [Fact]
    public async Task OnPost_WhenGuestNameIsWhitespace_FailsValidation()
    {
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        }, "TestAuth"));

        var model = CreateModel(userPrincipal, CreateMockCart());
        model.GuestName = "   ";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(nameof(CheckoutModel.GuestName)));
        Assert.Contains(model.ModelState[nameof(CheckoutModel.GuestName)]!.Errors, e => e.ErrorMessage.Contains("name"));
    }

    [Fact]
    public async Task OnPost_PreFilledValuesEdited_DoesNotChangeUserProfile()
    {
        var userId = "user-123";
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "AliceJohnson")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            PhoneNumber = "555-0101"
        };
        _userManager.GetUserAsync(userPrincipal).Returns(appUser);

        var model = CreateModel(userPrincipal, CreateMockCart());

        // User edited the pre-filled values
        model.GuestName = "Bob Johnson";
        model.GuestEmail = "bob@example.com";
        model.GuestPhone = "555-9999";
        model.SpecialInstructions = "No onions";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Orders/Index", redirect.PageName);

        // User profile was not modified in the store or manager
        await _userManager.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        Assert.Equal("Alice", appUser.FirstName);
        Assert.Equal("Johnson", appUser.LastName);
        Assert.Equal("alice@example.com", appUser.Email);
        Assert.Equal("555-0101", appUser.PhoneNumber);
    }

    [Fact]
    public async Task OnPost_WhenCartIsEmpty_RedirectsToMenu()
    {
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
        }, "TestAuth"));

        var emptyCart = new Order { Items = new List<OrderItem>() };
        var model = CreateModel(userPrincipal, emptyCart);
        model.GuestName = "Alice Johnson";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Menu/Index", redirect.PageName);
    }
}
