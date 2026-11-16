using BistroGo.Core.Models;
using BistroGo.Core.Scheduling;
using Xunit;

namespace BistroGo.Tests;

public class PickupSlotRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 12, 0, 0);
    private static readonly TimeSpan Lead = TimeSpan.FromMinutes(15);

    private static PickupTimeSlot SlotAt(DateTime start) => new()
    {
        SlotDate = DateOnly.FromDateTime(start),
        StartTime = TimeOnly.FromDateTime(start),
        EndTime = TimeOnly.FromDateTime(start).AddMinutes(30),
        MaxOrders = 10
    };

    [Theory]
    [InlineData(-60, false)] // started an hour ago
    [InlineData(0, false)]   // starting right now
    [InlineData(14, false)]  // inside the lead time
    [InlineData(15, true)]   // exactly at the lead time: still bookable
    [InlineData(16, true)]
    [InlineData(90, true)]
    public void IsBookable_DependsOnMinutesUntilStart(int minutesFromNow, bool expected)
    {
        var slot = SlotAt(Now.AddMinutes(minutesFromNow));

        Assert.Equal(expected, PickupSlotRules.IsBookable(slot, Now, Lead));
    }

    [Fact]
    public void IsBookable_TomorrowMorning_IsBookable()
    {
        var slot = SlotAt(Now.Date.AddDays(1).AddHours(9));

        Assert.True(PickupSlotRules.IsBookable(slot, Now, Lead));
    }

    [Fact]
    public void IsBookable_YesterdayEvening_IsNotBookable()
    {
        var slot = SlotAt(Now.Date.AddDays(-1).AddHours(19));

        Assert.False(PickupSlotRules.IsBookable(slot, Now, Lead));
    }

    [Fact]
    public void IsBookable_WithZeroLeadTime_SlotStartingNowIsBookable()
    {
        var slot = SlotAt(Now);

        Assert.True(PickupSlotRules.IsBookable(slot, Now, TimeSpan.Zero));
    }

    [Fact]
    public void IsBookable_WithNegativeLeadTime_Throws()
    {
        var slot = SlotAt(Now.AddHours(1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PickupSlotRules.IsBookable(slot, Now, TimeSpan.FromMinutes(-1)));
    }
}