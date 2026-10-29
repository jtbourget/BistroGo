using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using BistroGo.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Tests;

public class EfPickupTimeSlotRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BistroGoDBContext _db;
    private readonly EfPickupTimeSlotRepository _repo;
    private int _codeCounter;

    public EfPickupTimeSlotRepositoryTests()
    {
        // An in-memory database only lives as long as its connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BistroGoDBContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new BistroGoDBContext(options);
        _db.Database.EnsureCreated();
        _repo = new EfPickupTimeSlotRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static PickupTimeSlot NewSlot(int startHour = 12) => new()
    {
        SlotDate = new DateOnly(2026, 11, 5),
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(startHour, 30),
        MaxOrders = 2
    };

    private async Task AddPlacedOrderAsync(int slotId, int statusId, bool archived = false)
    {
        _db.Orders.Add(new Order
        {
            PickupSlotId = slotId,
            IsFinalized = true,
            FinalizedAt = DateTime.UtcNow,
            StatusId = statusId,
            ConfirmationCode = $"BG-TEST{++_codeCounter}",
            IsArchived = archived,
            ArchivedAt = archived ? DateTime.UtcNow : null
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task CountBookedOrders_CountsActiveAndCompletedOrders_NotCancelledOrOtherSlots()
    {
        var slotA = await _repo.AddAsync(NewSlot(12));
        var slotB = await _repo.AddAsync(NewSlot(13));

        await AddPlacedOrderAsync(slotA.Id, OrderStatusIds.Received);
        await AddPlacedOrderAsync(slotA.Id, OrderStatusIds.PickedUp, archived: true);
        await AddPlacedOrderAsync(slotA.Id, OrderStatusIds.Cancelled);
        await AddPlacedOrderAsync(slotB.Id, OrderStatusIds.Received);

        var count = await _repo.CountBookedOrdersAsync(slotA.Id);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountBookedOrders_IgnoresOpenCarts()
    {
        var slot = await _repo.AddAsync(NewSlot());
        _db.Orders.Add(new Order { PickupSlotId = slot.Id, IsFinalized = false });
        await _db.SaveChangesAsync();

        var count = await _repo.CountBookedOrdersAsync(slot.Id);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task AddAsync_AfterArchivingSlot_AllowsSameDateAndTime()
    {
        var first = await _repo.AddAsync(NewSlot());
        await _repo.DeleteAsync(first.Id);

        var second = await _repo.AddAsync(NewSlot());

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task AddAsync_DuplicateActiveSlot_Throws()
    {
        await _repo.AddAsync(NewSlot());

        await Assert.ThrowsAsync<DbUpdateException>(() => _repo.AddAsync(NewSlot()));
    }
}