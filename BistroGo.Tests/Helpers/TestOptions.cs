using BistroGo.Core.Scheduling;
using Microsoft.Extensions.Options;

namespace BistroGo.Tests;

/// <summary>Builds pickup-slot settings for tests, with a 15-minute lead time unless told otherwise.</summary>
internal static class TestOptions
{
    public static IOptions<PickupSlotOptions> Lead(int minutes = 15) =>
        Options.Create(new PickupSlotOptions { LeadTimeMinutes = minutes });
}