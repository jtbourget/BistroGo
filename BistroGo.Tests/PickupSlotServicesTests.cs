using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.Services;
using NSubstitute;

namespace BistroGo.Tests;

public class PickupSlotServiceTests
{
    // A clock frozen at noon on Nov 4, 2026 (UTC is also "local" for the test).
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly DateOnly Today = new(2026, 11, 4);
    private static readonly DateOnly Tomorrow = new(2026, 11, 5);

    private readonly IPickupTimeSlotRepository _slots = Substitute.For<IPickupTimeSlotRepository>();
    private readonly PickupSlotService _service;

    public PickupSlotServiceTests()
    {
        // Without this the fake returns null for the list, and the service would crash on it.
        _slots.GetByDateAsync(Arg.Any<DateOnly>()).Returns(new List<PickupTimeSlot>());
        _slots.AddAsync(Arg.Any<PickupTimeSlot>()).Returns(call => call.Arg<PickupTimeSlot>());

        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        _service = new PickupSlotService(_slots, clock, TestOptions.Lead());
    }

    private static PickupTimeSlot Slot(DateOnly date, int startHour = 12, int endHour = 13, int max = 5) => new()
    {
        SlotDate = date,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0),
        MaxOrders = max
    };

    private static PickupTimeSlot SlotAt(DateOnly date, int hour, int minute) => new()
    {
        SlotDate = date,
        StartTime = new TimeOnly(hour, minute),
        EndTime = new TimeOnly(hour, minute).AddMinutes(30),
        MaxOrders = 5
    };

    private static BulkSlotRequest Day(string opens, string closes, int minutes = 30, int max = 10, DateOnly? date = null) =>
        new(date ?? Tomorrow, TimeOnly.Parse(opens), TimeOnly.Parse(closes), minutes, max);

    // ── CreateAsync ──

    [Fact]
    public async Task Create_InThePast_IsRefused()
    {
        var outcome = await _service.CreateAsync(Slot(Today.AddDays(-1)));

        Assert.Equal(CreateSlotResult.DateInPast, outcome.Result);
        await _slots.DidNotReceive().AddAsync(Arg.Any<PickupTimeSlot>());
    }

    [Fact]
    public async Task Create_WhenEndIsNotAfterStart_IsRefused()
    {
        var outcome = await _service.CreateAsync(Slot(Tomorrow, startHour: 13, endHour: 13));

        Assert.Equal(CreateSlotResult.EndNotAfterStart, outcome.Result);
    }

    [Fact]
    public async Task Create_WithZeroCapacity_IsRefused()
    {
        var outcome = await _service.CreateAsync(Slot(Tomorrow, max: 0));

        Assert.Equal(CreateSlotResult.InvalidCapacity, outcome.Result);
    }

    [Fact]
    public async Task Create_WhenTheStartTimeIsTaken_IsRefusedAsDuplicate()
    {
        _slots.GetByDateAsync(Tomorrow).Returns(new List<PickupTimeSlot> { Slot(Tomorrow, 12, 13) });

        var outcome = await _service.CreateAsync(Slot(Tomorrow, 12, 14));

        Assert.Equal(CreateSlotResult.Duplicate, outcome.Result);
        await _slots.DidNotReceive().AddAsync(Arg.Any<PickupTimeSlot>());
    }

    [Fact]
    public async Task Create_WhenValid_SavesAFreshSlot_IgnoringTheCallersId()
    {
        var input = Slot(Tomorrow);
        input.Id = 77;   // a caller trying to choose its own Id

        var outcome = await _service.CreateAsync(input);

        Assert.Equal(CreateSlotResult.Created, outcome.Result);
        Assert.Equal(0, outcome.Slot!.Id);
        Assert.Equal(Tomorrow, outcome.Slot.SlotDate);
    }

    // ── CreateDayAsync ──

    [Fact]
    public async Task CreateDay_InThePast_IsRefused()
    {
        var outcome = await _service.CreateDayAsync(Day("11:00", "13:00", date: Today.AddDays(-1)));

        Assert.Equal(BulkSlotResult.DateInPast, outcome.Result);
    }

    [Fact]
    public async Task CreateDay_WhenClosingIsNotAfterOpening_IsRefused()
    {
        var outcome = await _service.CreateDayAsync(Day("13:00", "11:00"));

        Assert.Equal(BulkSlotResult.InvalidHours, outcome.Result);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(241)]
    public async Task CreateDay_WithAnUnreasonableSlotLength_IsRefused(int minutes)
    {
        var outcome = await _service.CreateDayAsync(Day("11:00", "13:00", minutes));

        Assert.Equal(BulkSlotResult.InvalidSlotLength, outcome.Result);
    }

    [Fact]
    public async Task CreateDay_WithZeroCapacity_IsRefused()
    {
        var outcome = await _service.CreateDayAsync(Day("11:00", "13:00", max: 0));

        Assert.Equal(BulkSlotResult.InvalidCapacity, outcome.Result);
    }

    [Fact]
    public async Task CreateDay_BuildsBackToBackSlotsBetweenOpeningAndClosing()
    {
        var outcome = await _service.CreateDayAsync(Day("11:00", "13:00"));

        Assert.Equal(BulkSlotResult.Created, outcome.Result);
        Assert.Equal(4, outcome.Created.Count);
        Assert.Equal(new TimeOnly(11, 0), outcome.Created[0].StartTime);
        Assert.Equal(new TimeOnly(11, 30), outcome.Created[0].EndTime);
        Assert.Equal(new TimeOnly(13, 0), outcome.Created[3].EndTime);
        Assert.All(outcome.Created, s => Assert.Equal(10, s.MaxOrders));
        await _slots.Received(1).AddRangeAsync(Arg.Any<IEnumerable<PickupTimeSlot>>());
    }

    [Fact]
    public async Task CreateDay_DropsALastSlotThatWouldRunPastClosing()
    {
        var outcome = await _service.CreateDayAsync(Day("11:00", "12:45"));

        Assert.Equal(3, outcome.Created.Count);   // 11:00, 11:30, 12:00; 12:30-13:00 doesn't fit
        Assert.Equal(new TimeOnly(12, 30), outcome.Created[^1].EndTime);
    }

    [Fact]
    public async Task CreateDay_SkipsSlotsThatAlreadyExist_AndCreatesTheRest()
    {
        _slots.GetByDateAsync(Tomorrow).Returns(new List<PickupTimeSlot>
        {
            new() { SlotDate = Tomorrow, StartTime = new TimeOnly(11, 30), EndTime = new TimeOnly(12, 0), MaxOrders = 3 }
        });

        var outcome = await _service.CreateDayAsync(Day("11:00", "13:00"));

        Assert.Equal(3, outcome.Created.Count);
        Assert.Equal(1, outcome.Skipped);
        Assert.DoesNotContain(outcome.Created, s => s.StartTime == new TimeOnly(11, 30));
    }

    [Fact]
    public async Task CreateDay_WhenEverySlotExists_SavesNothing()
    {
        _slots.GetByDateAsync(Tomorrow).Returns(new List<PickupTimeSlot>
        {
            new() { SlotDate = Tomorrow, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(11, 30), MaxOrders = 3 },
            new() { SlotDate = Tomorrow, StartTime = new TimeOnly(11, 30), EndTime = new TimeOnly(12, 0), MaxOrders = 3 }
        });

        var outcome = await _service.CreateDayAsync(Day("11:00", "12:00"));

        Assert.Empty(outcome.Created);
        Assert.Equal(2, outcome.Skipped);
        await _slots.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<PickupTimeSlot>>());
    }

    [Fact]
    public async Task CreateDay_NearMidnight_DoesNotWrapAroundToTheNextMorning()
    {
        var outcome = await _service.CreateDayAsync(Day("23:00", "23:59"));

        Assert.Single(outcome.Created);   // 23:00-23:30; a 23:30-00:00 slot must NOT appear
        Assert.Equal(new TimeOnly(23, 30), outcome.Created[0].EndTime);
    }

    // ── GetAvailableAsync ──

    [Fact]
    public async Task GetAvailable_ForAPastDay_ReturnsNothing()
    {
        var result = await _service.GetAvailableAsync(Today.AddDays(-1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailable_Today_SkipsSlotsThatHaveAlreadyStarted()
    {
        // The clock says 12:00. 11:00 and 12:00 have started; 13:00 hasn't.
        _slots.GetByDateAsync(Today).Returns(new List<PickupTimeSlot>
    {
        Slot(Today, 11, 12), Slot(Today, 12, 13), Slot(Today, 13, 14)
    });

        var result = await _service.GetAvailableAsync(Today);

        var only = Assert.Single(result);
        Assert.Equal(new TimeOnly(13, 0), only.Slot.StartTime);
    }

    [Fact]
    public async Task GetAvailable_HidesFullSlots_AndReportsSpotsRemaining()
    {
        var full = Slot(Tomorrow, 11, 12, max: 5);
        full.Id = 1;
        var partly = Slot(Tomorrow, 12, 13, max: 5);
        partly.Id = 2;
        _slots.GetByDateAsync(Tomorrow).Returns(new List<PickupTimeSlot> { full, partly });
        _slots.CountBookedOrdersAsync(1).Returns(5);
        _slots.CountBookedOrdersAsync(2).Returns(2);

        var result = await _service.GetAvailableAsync(Tomorrow);

        var only = Assert.Single(result);
        Assert.Equal(2, only.Slot.Id);
        Assert.Equal(3, only.Remaining);
    }

    [Fact]
    public async Task GetAvailable_WithNoDate_MeansToday()
    {
        _slots.GetByDateAsync(Today).Returns(new List<PickupTimeSlot> { Slot(Today, 13, 14) });

        var result = await _service.GetAvailableAsync(null);

        Assert.Single(result);
        await _slots.Received(1).GetByDateAsync(Today);
    }

    [Fact]
    public async Task GetAvailable_Today_HidesSlotsInsideTheLeadTime()
    {
        // Clock says 12:00; lead time is 15 minutes.
        _slots.GetByDateAsync(Today).Returns(new List<PickupTimeSlot>
    {
        SlotAt(Today, 12, 10),   // only 10 minutes away: hidden
        SlotAt(Today, 12, 15),   // exactly 15 minutes away: still bookable
        SlotAt(Today, 12, 30)
    });

        var result = await _service.GetAvailableAsync(Today);

        Assert.Equal(
            new[] { new TimeOnly(12, 15), new TimeOnly(12, 30) },
            result.Select(a => a.Slot.StartTime));
    }

    [Fact]
    public async Task GetAvailable_UsesTheConfiguredLeadTime()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        var service = new PickupSlotService(_slots, clock, TestOptions.Lead(60));
        _slots.GetByDateAsync(Today).Returns(new List<PickupTimeSlot>
    {
        SlotAt(Today, 12, 30),   // 30 minutes away: hidden with a 60-minute lead
        SlotAt(Today, 13, 0)     // exactly 60 minutes away: bookable
    });

        var result = await service.GetAvailableAsync(Today);

        var only = Assert.Single(result);
        Assert.Equal(new TimeOnly(13, 0), only.Slot.StartTime);
    }

    [Fact]
    public async Task GetUpcoming_CombinesTheDaysInOrder()
    {
        _slots.GetByDateAsync(Today).Returns(new List<PickupTimeSlot> { SlotAt(Today, 13, 0) });
        _slots.GetByDateAsync(Tomorrow).Returns(new List<PickupTimeSlot> { SlotAt(Tomorrow, 9, 0) });

        var result = await _service.GetUpcomingAsync(2);

        Assert.Equal(new[] { Today, Tomorrow }, result.Select(a => a.Slot.SlotDate));
    }

    [Fact]
    public async Task GetUpcoming_OnlyLooksAsFarAsRequested()
    {
        await _service.GetUpcomingAsync(1);

        await _slots.Received(1).GetByDateAsync(Today);
        await _slots.DidNotReceive().GetByDateAsync(Tomorrow);
    }

    [Fact]
    public async Task GetUpcoming_WithZeroDays_ReturnsNothing()
    {
        var result = await _service.GetUpcomingAsync(0);

        Assert.Empty(result);
    }
}