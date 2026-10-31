using BistroGo.Api.Controllers;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace BistroGo.Tests;

public class OrdersControllerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IOrderService _service = Substitute.For<IOrderService>();
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _controller = new OrdersController(_repo, _service);
    }

    // ── PlaceOrder: the controller only maps the service's answer to HTTP ──

    [Fact]
    public async Task PlaceOrder_WhenPlaced_ReturnsOk()
    {
        _service.PlaceOrderAsync(1).Returns(PlaceOrderResult.Placed);

        var result = await _controller.PlaceOrder(1);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PlaceOrder_WhenNotFound_Returns404()
    {
        _service.PlaceOrderAsync(1).Returns(PlaceOrderResult.NotFound);

        var result = await _controller.PlaceOrder(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PlaceOrder_WhenAlreadyPlaced_Returns409WithProblemDetails()
    {
        _service.PlaceOrderAsync(1).Returns(PlaceOrderResult.AlreadyPlaced);

        var result = await _controller.PlaceOrder(1);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, problem.Status);
    }

    [Fact]
    public async Task PlaceOrder_WithoutPickupSlot_Returns400WithProblemDetails()
    {
        _service.PlaceOrderAsync(1).Returns(PlaceOrderResult.PickupSlotRequired);

        var result = await _controller.PlaceOrder(1);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task PlaceOrder_WhenSlotFull_Returns409()
    {
        _service.PlaceOrderAsync(1).Returns(PlaceOrderResult.SlotFull);

        var result = await _controller.PlaceOrder(1);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, problem.Status);
    }

    [Fact]
    public async Task PlaceOrder_WhenSlotUnavailable_Returns400()
    {
        _service.PlaceOrderAsync(1).Returns(PlaceOrderResult.SlotUnavailable);

        var result = await _controller.PlaceOrder(1);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Equal(400, problem.Status);
    }

    // ── GetById: a missing order is a 404, a found one is returned ──

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        _repo.GetByIdAsync(5).Returns((Order?)null);

        var result = await _controller.GetById(5);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsThatOrder()
    {
        var order = new Order { Id = 5 };
        _repo.GetByIdAsync(5).Returns(order);

        var result = await _controller.GetById(5);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(order, ok.Value);
    }

    [Fact]
    public async Task AddLine_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        _repo.GetByIdAsync(99).Returns((Order?)null);

        // Act
        var result = await _controller.AddLine(99, new OrderItem());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        await _repo.DidNotReceive().AddLineAsync(Arg.Any<OrderItem>());
    }
}