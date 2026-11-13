using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Core.Scheduling;
using BistroGo.Data.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BistroGo.Tests;

public class OrderServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 4);
    private static readonly DateOnly Tomorrow = new(2026, 11, 5);

    // A fake repository: it implements IOrderRepository but only does what each test tells it to.
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();

    private readonly IPickupTimeSlotRepository _slots = Substitute.For<IPickupTimeSlotRepository>();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        // The clock is frozen at noon on Nov 4, 2026, with a 15-minute lead time.
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        _service = new OrderService(_repo, _slots, clock, Options.Create(new PickupSlotOptions { LeadTimeMinutes = 15 }));
    }

    // By default the slot is tomorrow at noon, comfortably in the future.
    private void GivenSlot(int slotId, int maxOrders, int booked, DateOnly? date = null, TimeOnly? start = null)
    {
        _slots.GetByIdAsync(slotId).Returns(new PickupTimeSlot
        {
            Id = slotId,
            MaxOrders = maxOrders,
            SlotDate = date ?? Tomorrow,
            StartTime = start ?? new TimeOnly(12, 0)
        });
        _slots.CountBookedOrdersAsync(slotId).Returns(booked);
    }

    [Fact]
    public async Task PlaceOrder_WhenSlotDoesNotExist_ReturnsSlotUnavailable()
    {
        _repo.GetByIdAsync(1).Returns(new Order { Id = 1, PickupSlotId = 3 });
        _slots.GetByIdAsync(3).Returns((PickupTimeSlot?)null);

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.SlotUnavailable, result);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WhenSlotIsFull_ReturnsSlotFullAndLeavesTheOrderAlone()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        GivenSlot(3, maxOrders: 2, booked: 2);

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.SlotFull, result);
        Assert.False(order.IsFinalized);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WhenOneSpotIsLeft_Places()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateIfSlotHasRoomAsync(order, Arg.Any<int>()).Returns(SlotSaveResult.Saved);
        GivenSlot(3, maxOrders: 2, booked: 1);

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.Placed, result);
    }

    [Fact]
    public async Task PlaceOrder_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        _repo.GetByIdAsync(99).Returns((Order?)null);

        var result = await _service.PlaceOrderAsync(99);

        Assert.Equal(PlaceOrderResult.NotFound, result);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WhenAlreadyFinalized_ReturnsAlreadyPlaced()
    {
        _repo.GetByIdAsync(1).Returns(new Order { Id = 1, IsFinalized = true, PickupSlotId = 3 });

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.AlreadyPlaced, result);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WithoutPickupSlot_ReturnsPickupSlotRequired()
    {
        _repo.GetByIdAsync(1).Returns(new Order { Id = 1, PickupSlotId = null });

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.PickupSlotRequired, result);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WhenValid_FinalizesTheOrderAndSavesIt()
    {
        GivenSlot(3, maxOrders: 10, booked: 0);

        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateIfSlotHasRoomAsync(order, Arg.Any<int>()).Returns(SlotSaveResult.Saved);

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.Placed, result);
        Assert.True(order.IsFinalized);
        Assert.NotNull(order.FinalizedAt);
        Assert.Equal(OrderStatusIds.Received, order.StatusId);
        Assert.StartsWith("BG-", order.ConfirmationCode);
        await _repo.Received(1).UpdateIfSlotHasRoomAsync(order, 10);
    }

    [Fact]
    public async Task PlaceOrder_WhenTheSlotFillsUpDuringTheSave_ReturnsSlotFull()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        GivenSlot(3, maxOrders: 2, booked: 1);   // looks fine at first...
        _repo.UpdateIfSlotHasRoomAsync(order, 2).Returns(SlotSaveResult.SlotFull);   // ...but someone got there first

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.SlotFull, result);
    }

    [Fact]
    public async Task PlaceOrder_KeepsAnExistingConfirmationCode()
    {
        GivenSlot(3, maxOrders: 10, booked: 0);
        var order = new Order { Id = 1, PickupSlotId = 3, ConfirmationCode = "BG-ABC123" };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateIfSlotHasRoomAsync(order, Arg.Any<int>()).Returns(SlotSaveResult.Saved);

        await _service.PlaceOrderAsync(1);

        Assert.Equal("BG-ABC123", order.ConfirmationCode);
    }

    [Fact]
    public async Task PlaceOrder_WhenTheSaveFindsNoOrder_ReturnsNotFound()
    {
        GivenSlot(3, maxOrders: 10, booked: 0);
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateIfSlotHasRoomAsync(order, Arg.Any<int>()).Returns(SlotSaveResult.OrderNotFound);

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.NotFound, result);
    }

    [Fact]
    public async Task PlaceOrder_WhenTheSlotHasAlreadyStarted_ReturnsSlotUnavailable()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        GivenSlot(3, maxOrders: 10, booked: 0, date: Today, start: new TimeOnly(11, 0));   // the clock says 12:00

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.SlotUnavailable, result);
        Assert.False(order.IsFinalized);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WhenTheSlotIsInsideTheLeadTime_ReturnsSlotUnavailable()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        GivenSlot(3, maxOrders: 10, booked: 0, date: Today, start: new TimeOnly(12, 10));   // only 10 minutes away

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.SlotUnavailable, result);
        await _repo.DidNotReceive().UpdateIfSlotHasRoomAsync(Arg.Any<Order>(), Arg.Any<int>());
    }

    [Fact]
    public async Task PlaceOrder_WhenTheSlotIsExactlyAtTheLeadTime_Places()
    {
        var order = new Order { Id = 1, PickupSlotId = 3 };
        _repo.GetByIdAsync(1).Returns(order);
        _repo.UpdateIfSlotHasRoomAsync(order, Arg.Any<int>()).Returns(SlotSaveResult.Saved);
        GivenSlot(3, maxOrders: 10, booked: 0, date: Today, start: new TimeOnly(12, 15));   // exactly 15 minutes away

        var result = await _service.PlaceOrderAsync(1);

        Assert.Equal(PlaceOrderResult.Placed, result);
    }
}