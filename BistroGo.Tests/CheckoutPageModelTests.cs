using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Identity;
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

    private CheckoutModel CreateModel(ClaimsPrincipal? user = null, Order? cart = null)
    {
        user ??= new ClaimsPrincipal(new ClaimsIdentity()); // Default to unauthenticated guest
        var httpContext = new DefaultHttpContext { User = user };

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            var guestToken = Guid.NewGuid();
            httpContext.Request.Headers.Cookie = $"BistroGo.GuestToken={guestToken}";
        }

        cart ??= new Order
        {
            Items = new List<OrderItem>
            {
                new()
                {
                    MenuItemId = 1,
                    Quantity = 2,
                    UnitPrice = 10.00m,
                    MenuItem = new MenuItem { Name = "Burger" }
                }
            }
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(cart);

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        return new CheckoutModel(_cart, _userManager)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnGetAsync_WhenCartEmpty_RedirectsToMenuIndex()
    {
        var emptyCart = new Order { Items = new List<OrderItem>() };
        var model = CreateModel(cart: emptyCart);

        var result = await model.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Menu/Index", redirect.PageName);
        Assert.Equal("Your cart is empty. Add items from the menu first.", model.TempData["Message"]);
    }

    [Fact]
    public async Task OnGetAsync_WhenGuestUser_LeavesGuestFieldsNull()
    {
        var guestUser = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        var model = CreateModel(user: guestUser);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(model.GuestName);
        Assert.Null(model.GuestEmail);
        Assert.Null(model.GuestPhone);
        await _userManager.DidNotReceive().GetUserAsync(Arg.Any<ClaimsPrincipal>());
    }

    [Fact]
    public async Task OnGetAsync_WhenAuthenticatedUser_PreFillsNameEmailAndPhone()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "alice@example.com")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = "user-123",
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            PhoneNumber = "555-0199"
        };

        _userManager.GetUserAsync(user).Returns(appUser);

        var model = CreateModel(user: user);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Alice Johnson", model.GuestName);
        Assert.Equal("alice@example.com", model.GuestEmail);
        Assert.Equal("555-0199", model.GuestPhone);
    }

    [Fact]
    public async Task OnGetAsync_WhenAuthenticatedUserHasNoPhone_LeavesPhoneNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-456"),
            new Claim(ClaimTypes.Name, "bob@example.com")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = "user-456",
            FirstName = "Bob",
            LastName = "Smith",
            Email = "bob@example.com",
            PhoneNumber = null
        };

        _userManager.GetUserAsync(user).Returns(appUser);

        var model = CreateModel(user: user);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Bob Smith", model.GuestName);
        Assert.Equal("bob@example.com", model.GuestEmail);
        Assert.Null(model.GuestPhone);
    }

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageAndPreservesTypedValues()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "alice@example.com")
        }, "TestAuth"));

        var profileUser = new ApplicationUser
        {
            Id = "user-123",
            FirstName = "ProfileFirst",
            LastName = "ProfileLast",
            Email = "profile@example.com",
            PhoneNumber = "555-0000"
        };
        _userManager.GetUserAsync(user).Returns(profileUser);

        var model = CreateModel(user: user);

        // User typed these custom values into the form
        model.GuestName = "Custom Typed Name";
        model.GuestEmail = "invalid-email-address";
        model.GuestPhone = "555-9999";
        model.SpecialInstructions = "No pickles";

        // Simulate model validation failure on email
        model.ModelState.AddModelError(nameof(CheckoutModel.GuestEmail), "The Email field is not a valid e-mail address.");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        // Verify values are NOT replaced with profile values
        Assert.Equal("Custom Typed Name", model.GuestName);
        Assert.Equal("invalid-email-address", model.GuestEmail);
        Assert.Equal("555-9999", model.GuestPhone);
        Assert.Equal("No pickles", model.SpecialInstructions);
    }

    [Fact]
    public async Task OnPostAsync_WhenValid_RedirectsToOrdersIndexAndSetsTempData()
    {
        var model = CreateModel();
        model.GuestName = "Charlie Brown";
        model.GuestEmail = "charlie@example.com";
        model.GuestPhone = "555-1234";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Orders/Index", redirect.PageName);
        Assert.Equal("Order placed for Charlie Brown.", model.TempData["Message"]);
    }

    [Fact]
    public async Task OnPostAsync_WhenPreFilledValuesEdited_DoesNotModifyApplicationUser()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "alice@example.com")
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = "user-123",
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com"
        };
        _userManager.GetUserAsync(user).Returns(appUser);

        var model = CreateModel(user: user);

        // Customer edits prefilled values for this order
        model.GuestName = "Alice Different";
        model.GuestEmail = "different@example.com";
        model.GuestPhone = "555-8888";

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        // User profile in identity was never updated
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public void CheckoutModel_DataAnnotationAttributes_MatchExpectedOrderLimits()
    {
        var nameProp = typeof(CheckoutModel).GetProperty(nameof(CheckoutModel.GuestName));
        Assert.NotNull(nameProp);
        var requiredAttr = nameProp.GetCustomAttribute<RequiredAttribute>();
        Assert.NotNull(requiredAttr);
        var nameMaxLen = nameProp.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(nameMaxLen);
        Assert.Equal(100, nameMaxLen.Length);

        var emailProp = typeof(CheckoutModel).GetProperty(nameof(CheckoutModel.GuestEmail));
        Assert.NotNull(emailProp);
        var emailMaxLen = emailProp.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(emailMaxLen);
        Assert.Equal(256, emailMaxLen.Length);
        var emailAttr = emailProp.GetCustomAttribute<EmailAddressAttribute>();
        Assert.NotNull(emailAttr);

        var phoneProp = typeof(CheckoutModel).GetProperty(nameof(CheckoutModel.GuestPhone));
        Assert.NotNull(phoneProp);
        var phoneMaxLen = phoneProp.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(phoneMaxLen);
        Assert.Equal(20, phoneMaxLen.Length);
        var phoneAttr = phoneProp.GetCustomAttribute<PhoneAttribute>();
        Assert.NotNull(phoneAttr);
    }
}
