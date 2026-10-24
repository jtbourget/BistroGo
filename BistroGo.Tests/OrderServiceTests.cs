using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Services;
using NSubstitute;

namespace BistroGo.Tests;

public class OrderServiceTests
{
    // A fake repository: it implements IOrderRepository but only does what each test tells it to.
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(_repo);
    }

    [Fact]
    public async Task PlaceOrder_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        _repo.GetByIdAsync(99).Returns((Order?)null);

        var result = await _service.PlaceOrderAsync(99);

        Assert.Equal(PlaceOrderResult.NotFound, result);
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Order>());
    }

    [Fact]
    public async Task PlaceOrder_WhenAlreadyFinalized_ReturnsAlreadyPlaced()
    {
        _repo.GetByIdAsync(1).Returns(new Order { Id = 1, IsFinalized = true, PickupSlotId = 3 });

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.AlreadyPlaced, result);
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Order>());
    }

    [Fact]
    public async Task PlaceOrder_WithoutPickupSlot_ReturnsPickupSlotRequired()
    {
        _repo.GetByIdAsync(1).Returns(new Order { Id = 1, PickupSlotId = null });

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.PickupSlotRequired, result);
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Order>());
    }

    [Fact]
    public async Task PlaceOrder_WhenValid_FinalizesTheOrderAndSavesIt()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateAsync(order).Returns(true);

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.Placed, result);
        Assert.True(order.IsFinalized);
        Assert.NotNull(order.FinalizedAt);
        Assert.Equal(OrderStatusIds.Received, order.StatusId);
        Assert.StartsWith("BG-", order.ConfirmationCode);
        await _repo.Received(1).UpdateAsync(order);
    }

    [Fact]
    public async Task PlaceOrder_KeepsAnExistingConfirmationCode()
    {
        var order = new Order { Id = 1, PickupSlotId = 3, ConfirmationCode = "BG-ABC123" };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateAsync(order).Returns(true);

        await _service.PlaceOrderAsync(1);

        Assert.Equal("BG-ABC123", order.ConfirmationCode);
    }

    [Fact]
    public async Task PlaceOrder_WhenTheSaveFindsNoOrder_ReturnsNotFound()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateAsync(order).Returns(false);   // e.g. deleted between the load and the save

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.NotFound, result);
    }
}