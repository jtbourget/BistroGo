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

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
