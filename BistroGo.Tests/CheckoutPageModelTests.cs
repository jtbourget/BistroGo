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
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IOrderService _orderService = Substitute.For<IOrderService>();
    private readonly IPickupTimeSlotRepository _pickupRepo = Substitute.For<IPickupTimeSlotRepository>();
    private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutPageModelTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            _userStore, null, null, null, null, null, null, null, null);

        _userManager.GetUserId(Arg.Any<ClaimsPrincipal>())
            .Returns(callInfo => callInfo.Arg<ClaimsPrincipal>()?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        _orderRepo.UpdateAsync(Arg.Any<Order>()).Returns(true);
        _pickupRepo.GetAllAsync().Returns(new List<PickupTimeSlot>
        {
            new() { Id = 1, SlotDate = DateOnly.FromDateTime(DateTime.Today), StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) }
        });
    }

    private static (bool Success, string? Message, int? OrderId) ReadJson(IActionResult result)
    {
        var json = Assert.IsType<JsonResult>(result);
        var val = json.Value;
        if (val == null) return (false, null, null);

        var successProp = val.GetType().GetProperty("success")?.GetValue(val);
        var msgProp = val.GetType().GetProperty("message")?.GetValue(val)?.ToString();
        var orderIdProp = val.GetType().GetProperty("orderId")?.GetValue(val) as int?;

        return (successProp is bool s && s, msgProp, orderIdProp);
    }

    private CheckoutModel CreateModel(ClaimsPrincipal? user = null, Guid? guestToken = null)
    {
        user ??= new ClaimsPrincipal(new ClaimsIdentity());

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

        return new CheckoutModel(_cart, _orderRepo, _orderService, _pickupRepo, _userManager)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnPostPlaceOrderJsonAsync_GuestOrder_SavesDetailsAndPlacesOrder()
    {
        // Arrange
        var token = Guid.NewGuid();
        var model = CreateModel(guestToken: token);
        var cart = new Order
        {
            Id = 10,
            GuestToken = token,
            IsFinalized = false,
            Items = new List<OrderItem>
            {
                new() { Id = 1, OrderId = 10, MenuItemId = 3, Quantity = 2, UnitPrice = 14.00m, MenuItem = new MenuItem { Id = 3, Name = "Margherita Pizza" } }
            }
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(cart);
        _orderService.PlaceOrderAsync(cart.Id).Returns(PlaceOrderResult.Placed);

        // Act
        var result = await model.OnPostPlaceOrderJsonAsync(
            guestName: "Alice Guest",
            guestPhone: "555-1234",
            guestEmail: "alice@example.com",
            specialInstructions: "Extra crispy",
            pickupSlotId: 1);

        // Assert
        var (success, _, orderId) = ReadJson(result);
        Assert.True(success);
        Assert.Equal(10, orderId);

        await _orderRepo.Received(1).UpdateAsync(Arg.Is<Order>(o =>
            o.Id == 10 &&
            o.GuestName == "Alice Guest" &&
            o.GuestPhone == "555-1234" &&
            o.GuestEmail == "alice@example.com" &&
            o.PickupSlotId == 1 &&
            o.Items.First().SpecialInstructions == "Extra crispy"
        ));

        await _orderService.Received(1).PlaceOrderAsync(10);
    }

    [Fact]
    public async Task OnPostPlaceOrderJsonAsync_AuthenticatedCustomer_AssociatesUserIdAndDetails()
    {
        // Arrange
        var userId = "user-abc-123";
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "customer@bistrogo.com"),
            new Claim(ClaimTypes.Role, Roles.Customer)
        }, "TestAuth"));

        var appUser = new ApplicationUser
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "customer@bistrogo.com",
            PhoneNumber = "555-9876"
        };
        _userManager.GetUserAsync(userClaims).Returns(appUser);

        var model = CreateModel(user: userClaims);
        var cart = new Order
        {
            Id = 20,
            UserId = userId,
            IsFinalized = false,
            Items = new List<OrderItem>
            {
                new() { Id = 2, OrderId = 20, MenuItemId = 4, Quantity = 1, UnitPrice = 22.00m, MenuItem = new MenuItem { Id = 4, Name = "Grilled Salmon" } }
            }
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(cart);
        _orderService.PlaceOrderAsync(cart.Id).Returns(PlaceOrderResult.Placed);

        // Act - submit without providing explicit name, should fall back to user's registered name
        var result = await model.OnPostPlaceOrderJsonAsync(
            guestName: "",
            guestPhone: null,
            guestEmail: null,
            specialInstructions: "Sauce on the side",
            pickupSlotId: 1);

        // Assert
        var (success, _, orderId) = ReadJson(result);
        Assert.True(success);
        Assert.Equal(20, orderId);

        await _orderRepo.Received(1).UpdateAsync(Arg.Is<Order>(o =>
            o.Id == 20 &&
            o.UserId == userId &&
            o.GuestName == "John Doe" &&
            o.GuestEmail == "customer@bistrogo.com" &&
            o.GuestPhone == "555-9876" &&
            o.Items.First().SpecialInstructions == "Sauce on the side"
        ));

        await _orderService.Received(1).PlaceOrderAsync(20);
    }

    [Fact]
    public async Task OnPostPlaceOrderJsonAsync_WhenCartEmpty_ReturnsFailure()
    {
        // Arrange
        var token = Guid.NewGuid();
        var model = CreateModel(guestToken: token);
        var emptyCart = new Order { Id = 30, Items = new List<OrderItem>() };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(emptyCart);

        // Act
        var result = await model.OnPostPlaceOrderJsonAsync("Guest", null, null, null, 1);

        // Assert
        var (success, message, _) = ReadJson(result);
        Assert.False(success);
        Assert.Contains("empty", message, StringComparison.OrdinalIgnoreCase);

        await _orderService.DidNotReceive().PlaceOrderAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostPlaceOrderJsonAsync_GuestWithoutName_ReturnsFailure()
    {
        // Arrange
        var token = Guid.NewGuid();
        var model = CreateModel(guestToken: token);
        var cart = new Order
        {
            Id = 40,
            Items = new List<OrderItem>
            {
                new() { Id = 3, MenuItemId = 1, Quantity = 1 }
            }
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(cart);

        // Act
        var result = await model.OnPostPlaceOrderJsonAsync("   ", null, null, null, 1);

        // Assert
        var (success, message, _) = ReadJson(result);
        Assert.False(success);
        Assert.Contains("Name is required", message);

        await _orderService.DidNotReceive().PlaceOrderAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostPlaceOrderJsonAsync_WhenOrderAlreadyFinalized_PreventsDuplicateSubmission()
    {
        // Arrange
        var token = Guid.NewGuid();
        var model = CreateModel(guestToken: token);
        var finalizedCart = new Order
        {
            Id = 50,
            IsFinalized = true,
            Items = new List<OrderItem>
            {
                new() { Id = 4, MenuItemId = 1, Quantity = 1 }
            }
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(finalizedCart);

        // Act
        var result = await model.OnPostPlaceOrderJsonAsync("Guest", null, null, null, 1);

        // Assert
        var (success, message, _) = ReadJson(result);
        Assert.False(success);
        Assert.Contains("already been placed", message);

        await _orderService.DidNotReceive().PlaceOrderAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostPlaceOrderJsonAsync_WhenOrderServiceFails_ReportsFailure()
    {
        // Arrange
        var token = Guid.NewGuid();
        var model = CreateModel(guestToken: token);
        var cart = new Order
        {
            Id = 60,
            Items = new List<OrderItem>
            {
                new() { Id = 5, MenuItemId = 1, Quantity = 1 }
            }
        };

        _cart.GetCartAsync(Arg.Any<CartOwner>()).Returns(cart);
        _orderService.PlaceOrderAsync(cart.Id).Returns(PlaceOrderResult.NotFound);

        // Act
        var result = await model.OnPostPlaceOrderJsonAsync("Guest", null, null, null, 1);

        // Assert
        var (success, message, _) = ReadJson(result);
        Assert.False(success);
        Assert.Contains("not found", message, StringComparison.OrdinalIgnoreCase);
    }
}
