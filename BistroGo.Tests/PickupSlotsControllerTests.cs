using BistroGo.Api.Contracts;
using BistroGo.Api.Controllers;
using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace BistroGo.Tests;

public class PickupSlotsControllerTests
{
    private readonly IPickupTimeSlotRepository _repo = Substitute.For<IPickupTimeSlotRepository>();
    private readonly IPickupSlotService _service = Substitute.For<IPickupSlotService>();
    private readonly PickupSlotsController _controller;

    public PickupSlotsControllerTests()
    {
        _controller = new PickupSlotsController(_repo, _service);
    }

    private static PickupTimeSlot NewSlot(int id = 0) => new()
    {
        Id = id,
        SlotDate = new DateOnly(2026, 11, 5),
        StartTime = new TimeOnly(12, 0),
        EndTime = new TimeOnly(12, 30),
        MaxOrders = 5
    };

    private static BulkSlotRequest NewBulkRequest() =>
        new(new DateOnly(2026, 11, 5), new TimeOnly(11, 0), new TimeOnly(13, 0), 30, 10);

    // ── Post ──

    [Fact]
    public async Task Post_WhenCreated_Returns201PointingAtTheNewSlot()
    {
        var created = NewSlot(id: 7);
        _service.CreateAsync(Arg.Any<PickupTimeSlot>()).Returns(new CreateSlotOutcome(CreateSlotResult.Created, created));

        var result = await _controller.Post(NewSlot());

        var response = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(PickupSlotsController.GetById), response.ActionName);
        Assert.Same(created, response.Value);
    }

    [Fact]
    public async Task Post_WhenDuplicate_Returns409()
    {
        _service.CreateAsync(Arg.Any<PickupTimeSlot>()).Returns(new CreateSlotOutcome(CreateSlotResult.Duplicate, null));

        var result = await _controller.Post(NewSlot());

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, Assert.IsType<ProblemDetails>(conflict.Value).Status);
    }

    [Theory]
    [InlineData(CreateSlotResult.DateInPast)]
    [InlineData(CreateSlotResult.EndNotAfterStart)]
    [InlineData(CreateSlotResult.InvalidCapacity)]
    public async Task Post_WhenTheSlotIsInvalid_Returns400(CreateSlotResult why)
    {
        _service.CreateAsync(Arg.Any<PickupTimeSlot>()).Returns(new CreateSlotOutcome(why, null));

        var result = await _controller.Post(NewSlot());

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, Assert.IsType<ProblemDetails>(bad.Value).Status);
    }

    // ── PostBulk ──

    [Fact]
    public async Task PostBulk_WhenCreated_ReturnsTheCountsAndTheNewSlots()
    {
        var created = new List<PickupTimeSlot> { NewSlot(1), NewSlot(2), NewSlot(3) };
        _service.CreateDayAsync(Arg.Any<BulkSlotRequest>())
            .Returns(new BulkSlotOutcome(BulkSlotResult.Created, created, 1));

        var result = await _controller.PostBulk(NewBulkRequest());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<BulkSlotsResponse>(ok.Value);
        Assert.Equal(3, body.CreatedCount);
        Assert.Equal(1, body.SkippedCount);
    }

    [Theory]
    [InlineData(BulkSlotResult.DateInPast)]
    [InlineData(BulkSlotResult.InvalidHours)]
    [InlineData(BulkSlotResult.InvalidSlotLength)]
    [InlineData(BulkSlotResult.InvalidCapacity)]
    public async Task PostBulk_WhenTheRequestIsInvalid_Returns400(BulkSlotResult why)
    {
        _service.CreateDayAsync(Arg.Any<BulkSlotRequest>())
            .Returns(new BulkSlotOutcome(why, Array.Empty<PickupTimeSlot>(), 0));

        var result = await _controller.PostBulk(NewBulkRequest());

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, Assert.IsType<ProblemDetails>(bad.Value).Status);
    }

    // ── GetAvailable ──

    [Fact]
    public async Task GetAvailable_ReturnsEachSlotWithItsRemainingSpots()
    {
        IReadOnlyList<SlotAvailability> available = new[] { new SlotAvailability(NewSlot(4), 3) };
        _service.GetAvailableAsync(Arg.Any<DateOnly?>()).Returns(available);

        var result = await _controller.GetAvailable(new DateOnly(2026, 11, 5));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var slots = Assert.IsAssignableFrom<IEnumerable<AvailableSlotResponse>>(ok.Value).ToList();
        var only = Assert.Single(slots);
        Assert.Equal(4, only.Id);
        Assert.Equal(3, only.SpotsRemaining);
    }
}