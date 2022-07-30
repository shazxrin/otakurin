using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Tracking;
using Otakurin.Domain;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Tracking;

[TestClass]
public class GetAllBookTrackingsTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetAllBookTrackingsHandler? GetAllBookTrackingsHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeDiffUserId = Guid.NewGuid();
    private static readonly List<Guid> FakeBookIds = new ()
    {
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid()
    };

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBookTrackingsList = new List<BookTracking>()
        {
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(0),
                ChaptersRead = 100,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(1),
                ChaptersRead = 90,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(2),
                ChaptersRead = 80,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(3),
                ChaptersRead = 70,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Planning,
                Ownership = MediaTrackingOwnership.Loan
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(4),
                ChaptersRead = 60,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.Planning,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(5),
                ChaptersRead = 50,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Completed,
                Ownership = MediaTrackingOwnership.Loan
            },
            new()
            {
                UserId = FakeDiffUserId,
                BookId = FakeBookIds.ElementAt(5),
                ChaptersRead = 25,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Loan
            }
        };

        var fakeBooksList = new List<Book>()
        {
            new() { Id = FakeBookIds.ElementAt(0) },
            new() { Id = FakeBookIds.ElementAt(1) },
            new() { Id = FakeBookIds.ElementAt(2) },
            new() { Id = FakeBookIds.ElementAt(3) },
            new() { Id = FakeBookIds.ElementAt(4) },
            new() { Id = FakeBookIds.ElementAt(5) },
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.BookTrackings.AddRange(fakeBookTrackingsList);
        InMemDatabase.Books.AddRange(fakeBooksList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetAllBookTrackingsHandler = new GetAllBookTrackingsHandler(InMemDatabase);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }


    [TestMethod]
    public async Task GetAllBookTrackings_Default()
    {
        // Arrange
        var query = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
        };

        // Act
        var result = await GetAllBookTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
    }

    [TestMethod]
    public async Task GetAllBookTrackings_ByBookStatus()
    {
        // Arrange
        var queryCompleted = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Completed,
        };
        var queryInProgress = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.InProgress,
        };
        var queryPaused = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Paused,
        };
        var queryPlanning = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Planning,
        };

        // Act
        var resultCompleted = await GetAllBookTrackingsHandler!.Handle(queryCompleted, CancellationToken.None);
        var resultInProgress = await GetAllBookTrackingsHandler.Handle(queryInProgress, CancellationToken.None);
        var resultPaused = await GetAllBookTrackingsHandler.Handle(queryPaused, CancellationToken.None);
        var resultPlanning = await GetAllBookTrackingsHandler.Handle(queryPlanning, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, resultCompleted.TotalCount);
        Assert.AreEqual(2, resultInProgress.TotalCount);
        Assert.AreEqual(1, resultPaused.TotalCount);
        Assert.AreEqual(2, resultPlanning.TotalCount);
    }

    [TestMethod]
    public async Task GetAllBookTrackings_SortByChaptersRead()
    {
        // Arrange
        var query = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            SortByChaptersRead = true
        };

        // Act
        var result = await GetAllBookTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(50, result.Items.First().ChaptersRead);
        Assert.AreEqual(100, result.Items.Last().ChaptersRead);
    }

    [TestMethod]
    public async Task GetAllBookTrackings_SortByFormat()
    {
        // Arrange
        var query = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            SortByFormat = true
        };

        // Act
        var result = await GetAllBookTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(MediaTrackingFormat.Digital, result.Items.First().Format);
        Assert.AreEqual(MediaTrackingFormat.Physical, result.Items.Last().Format);
    }

    [TestMethod]
    public async Task GetAllBookTrackings_SortByOwnership()
    {
        // Arrange
        var query = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            SortByOwnership = true
        };

        // Act
        var result = await GetAllBookTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(MediaTrackingOwnership.Owned, result.Items.First().Ownership);
        Assert.AreEqual(MediaTrackingOwnership.Loan, result.Items.Last().Ownership);
    }
    
    [TestMethod]
    public async Task GetAllBookTrackings_SortByRecentlyModified()
    {
        // This test modifies the database and should be last to run.
        
        // Arrange
        var query = new GetAllBookTrackingsQuery
        {
            UserId = FakeUserId,
            SortByRecentlyModified = true
        };
        
        var recentlyModifiedBookIdList = new List<Guid>();
        var bookTrackings = await InMemDatabase!.BookTrackings
            .Where(gt => gt.UserId == FakeUserId)
            .ToListAsync();
        foreach (var bookTracking in bookTrackings)
        {
            recentlyModifiedBookIdList.Add(bookTracking.BookId);
            bookTracking.ChaptersRead += 1;
            InMemDatabase.Update(bookTracking);
            await InMemDatabase.SaveChangesAsync();
            await Task.Delay(1000);
        }

        // Act
        var result = await GetAllBookTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(recentlyModifiedBookIdList.Last(), result.Items.First().BookId);
        Assert.AreEqual(recentlyModifiedBookIdList.First(), result.Items.Last().BookId);
    }
}