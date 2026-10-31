using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using BistroGo.Data.Repositories;
using BistroGo.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Tests;

public class PlaceOrderConcurrencyTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"bistrogo-{Guid.NewGuid():N}.db");

    private BistroGoDBContext NewContext() =>
        new(new DbContextOptionsBuilder<BistroGoDBContext>()
            .UseSqlite($"Data Source={_path}")
            .Options);

    private static OrderService NewService(BistroGoDBContext db) =>
        new(new EfOrderRepository(db), new EfPickupTimeSlotRepository(db));

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();   // release the file so it can be deleted
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public async Task TwoCustomersRacingForTheLastSpot_OnlyOneIsPlaced()
    {
        int orderA, orderB;
        using (var setup = NewContext())
        {
            setup.Database.EnsureCreated();
            var slot = new PickupTimeSlot
            {
                SlotDate = new DateOnly(2026, 11, 5),
                StartTime = new TimeOnly(12, 0),
                EndTime = new TimeOnly(12, 30),
                MaxOrders = 1
            };
            setup.PickupTimeSlots.Add(slot);
            await setup.SaveChangesAsync();

            var a = new Order { PickupSlotId = slot.Id };
            var b = new Order { PickupSlotId = slot.Id };
            setup.Orders.AddRange(a, b);
            await setup.SaveChangesAsync();
            orderA = a.Id;
            orderB = b.Id;
        }

        using var db1 = NewContext();
        using var db2 = NewContext();

        var results = await Task.WhenAll(
            NewService(db1).PlaceOrderAsync(orderA),
            NewService(db2).PlaceOrderAsync(orderB));

        Assert.Single(results, r => r == PlaceOrderResult.Placed);
        Assert.Single(results, r => r == PlaceOrderResult.SlotFull);
    }
}