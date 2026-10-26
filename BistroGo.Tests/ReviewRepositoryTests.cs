using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using BistroGo.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Tests;

public class ReviewRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BistroGoDBContext _db;
    private readonly EfReviewRepository _repo;

    public ReviewRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BistroGoDBContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new BistroGoDBContext(options);
        _db.Database.EnsureCreated();

        _repo = new EfReviewRepository(_db);
    }

    [Fact]
    public async Task AddAndGetAllAsync_IncludesCommentsAndOrdersDescending()
    {
        var r1 = new Review
        {
            Rating = 5,
            ReviewText = "First Review",
            CustomerName = "Alice",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var r2 = new Review
        {
            Rating = 4,
            ReviewText = "Second Review",
            CustomerName = "Bob",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(r1);
        await _repo.AddAsync(r2);

        var c1 = new ReviewComment
        {
            ReviewId = r2.Id,
            ManagerName = "Store Manager",
            CommentText = "Thank you Bob!"
        };
        await _repo.AddCommentAsync(c1);

        var reviews = await _repo.GetAllAsync();

        Assert.Equal(2, reviews.Count);
        // Ordered by CreatedAt descending
        Assert.Equal("Second Review", reviews[0].ReviewText);
        Assert.Single(reviews[0].Comments);
        Assert.Equal("Store Manager", reviews[0].Comments[0].ManagerName);

        Assert.Equal("First Review", reviews[1].ReviewText);
        Assert.Empty(reviews[1].Comments);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesReview()
    {
        var r = new Review
        {
            Rating = 3,
            ReviewText = "Okay food",
            CustomerName = "Charlie"
        };
        await _repo.AddAsync(r);

        var deleted = await _repo.DeleteAsync(r.Id);
        Assert.True(deleted);

        // GetAllAsync filters out archived reviews due to query filter
        var all = await _repo.GetAllAsync();
        Assert.DoesNotContain(all, x => x.Id == r.Id);
    }

    [Fact]
    public void Review_LocalCreatedAt_ConvertsUtcAndUnspecifiedToLocalTime()
    {
        var utcNow = DateTime.UtcNow;
        var reviewUtc = new Review { CreatedAt = utcNow };
        Assert.Equal(utcNow.ToLocalTime(), reviewUtc.LocalCreatedAt);

        // SQLite returns DateTimeKind.Unspecified
        var unspecified = DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified);
        var reviewUnspecified = new Review { CreatedAt = unspecified };
        Assert.Equal(utcNow.ToLocalTime(), reviewUnspecified.LocalCreatedAt);

        // If already local
        var localNow = DateTime.Now;
        var reviewLocal = new Review { CreatedAt = localNow };
        Assert.Equal(localNow, reviewLocal.LocalCreatedAt);
    }

    [Fact]
    public void ReviewComment_LocalCreatedAt_ConvertsUtcAndUnspecifiedToLocalTime()
    {
        var utcNow = DateTime.UtcNow;
        var commentUtc = new ReviewComment { CreatedAt = utcNow };
        Assert.Equal(utcNow.ToLocalTime(), commentUtc.LocalCreatedAt);

        var unspecified = DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified);
        var commentUnspecified = new ReviewComment { CreatedAt = unspecified };
        Assert.Equal(utcNow.ToLocalTime(), commentUnspecified.LocalCreatedAt);

        var localNow = DateTime.Now;
        var commentLocal = new ReviewComment { CreatedAt = localNow };
        Assert.Equal(localNow, commentLocal.LocalCreatedAt);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
