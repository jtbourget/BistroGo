using System.Reflection;
using System.Security.Claims;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Identity;
using BistroGo.Web.Pages.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using Xunit;

namespace BistroGo.Tests;

public class OrdersIndexPageModelTests
{
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersIndexPageModelTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            _userStore, null, null, null, null, null, null, null, null);
    }

    private IndexModel CreateModel(ClaimsPrincipal? user = null)
    {
        user ??= new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "cook-1"),
            new Claim(ClaimTypes.Name, "cook@bistrogo.com"),
            new Claim(ClaimTypes.Role, Roles.KitchenStaff)
        }, "TestAuth"));

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

        return new IndexModel(_orderRepo, _userManager)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public void IndexModel_HasAuthorizeAttributeForManagerAndKitchenStaff()
    {
        // Assert
        var authAttr = typeof(IndexModel).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Contains(Roles.Manager, authAttr.Roles);
        Assert.Contains(Roles.KitchenStaff, authAttr.Roles);
    }

    [Fact]
    public async Task OnGetAsync_RetrievesFinalizedOrdersWithItemsAndCustomizations()
    {
        // Arrange
        var orders = new List<Order>
        {
            new()
            {
                Id = 1,
                IsFinalized = true,
                GuestName = "Bob Smith",
                GuestPhone = "555-1111",
                PickupSlotId = 2,
                ConfirmationCode = "BG-ABC123",
                StatusId = OrderStatusIds.Received,
                Status = new OrderStatus { Id = OrderStatusIds.Received, Name = "Received" },
                Total = 28.00m,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                FinalizedAt = DateTime.UtcNow.AddMinutes(-5),
                Items = new List<OrderItem>
                {
                    new()
                    {
                        Id = 101,
                        MenuItemId = 5,
                        Quantity = 1,
                        UnitPrice = 28.00m,
                        SpecialInstructions = "Medium Rare",
                        MenuItem = new MenuItem { Id = 5, Name = "Ribeye Steak" }
                    }
                }
            },
            new()
            {
                Id = 2,
                IsFinalized = false // Cart that is not finalized should not appear
            }
        };

        _orderRepo.GetAllAsync().Returns(orders);
        var model = CreateModel();

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Single(model.CurrentOrders);
        var orderDto = model.CurrentOrders[0];
        Assert.Equal(1, orderDto.Id);
        Assert.Equal("Bob Smith", orderDto.CustomerName);
        Assert.Equal("Received", orderDto.Status);
        Assert.Equal("BG-ABC123", orderDto.ConfirmationCode);
        Assert.Equal(2, orderDto.TableNumber);
        Assert.Equal(28.00m, orderDto.Total);

        // Verify Order Items and Customizations
        Assert.Single(orderDto.Items);
        var itemDto = orderDto.Items[0];
        Assert.Equal(5, itemDto.MenuItemId);
        Assert.Equal("Ribeye Steak", itemDto.MenuItemName);
        Assert.Equal(1, itemDto.Quantity);
        Assert.Equal(28.00m, itemDto.UnitPrice);
        Assert.Equal("Medium Rare", itemDto.SpecialInstructions);
    }

    [Fact]
    public async Task OnGetAsync_DistinguishesOrderStatusesAndCounters()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = 1, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Received, Name = "Received" } },
            new() { Id = 2, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Preparing, Name = "Preparing" } },
            new() { Id = 3, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Ready, Name = "Ready" } },
            new() { Id = 4, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.PickedUp, Name = "PickedUp" } },
            new() { Id = 5, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Cancelled, Name = "Cancelled" } }
        };

        _orderRepo.GetAllAsync().Returns(orders);
        var model = CreateModel();

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(5, model.AllCount);
        Assert.Equal(1, model.ReceivedCount);
        Assert.Equal(1, model.PreparingCount);
        Assert.Equal(1, model.ReadyCount);
        Assert.Equal(1, model.PickedUpCount);
        Assert.Equal(1, model.CancelledCount);
    }

    [Fact]
    public async Task OnGetAsync_WithStatusFilter_FiltersOrdersCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = 1, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Received, Name = "Received" } },
            new() { Id = 2, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Preparing, Name = "Preparing" } },
            new() { Id = 3, IsFinalized = true, Status = new OrderStatus { Id = OrderStatusIds.Preparing, Name = "Preparing" } }
        };

        _orderRepo.GetAllAsync().Returns(orders);
        var model = CreateModel();
        model.StatusFilter = "Preparing";

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(2, model.CurrentOrders.Count);
        Assert.All(model.CurrentOrders, o => Assert.Equal("Preparing", o.Status));
    }

    [Theory]
    [InlineData("Preparing", OrderStatusIds.Preparing)]
    [InlineData("Ready", OrderStatusIds.Ready)]
    [InlineData("Received", OrderStatusIds.Received)]
    [InlineData("PickedUp", OrderStatusIds.PickedUp)]
    [InlineData("Completed", OrderStatusIds.PickedUp)]
    [InlineData("Cancelled", OrderStatusIds.Cancelled)]
    public async Task OnPostSetStatusAsync_UpdatesStatusCorrectly(string requestedStatus, int expectedStatusId)
    {
        // Arrange
        var order = new Order { Id = 10, StatusId = OrderStatusIds.Received };
        _orderRepo.GetByIdAsync(10).Returns(order);
        _orderRepo.UpdateAsync(order).Returns(true);
        var model = CreateModel();

        // Act
        var result = await model.OnPostSetStatusAsync(10, requestedStatus);

        // Assert
        Assert.Equal(expectedStatusId, order.StatusId);
        await _orderRepo.Received(1).UpdateAsync(order);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostCompleteAsync_SetsStatusToPickedUp()
    {
        // Arrange
        var order = new Order { Id = 15, StatusId = OrderStatusIds.Ready };
        _orderRepo.GetByIdAsync(15).Returns(order);
        _orderRepo.UpdateAsync(order).Returns(true);
        var model = CreateModel();

        // Act
        var result = await model.OnPostCompleteAsync(15);

        // Assert
        Assert.Equal(OrderStatusIds.PickedUp, order.StatusId);
        await _orderRepo.Received(1).UpdateAsync(order);
        Assert.IsType<RedirectToPageResult>(result);
    }
}
