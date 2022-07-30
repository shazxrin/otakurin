using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Shows.Tracking;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Shows.Tracking;

[TestClass]
public class GetAllShowTrackingsTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetAllShowTrackingsHandler? GetAllShowTrackingsHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeDiffUserId = Guid.NewGuid();
    private static readonly List<Guid> FakeShowIds = new ()
    {
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid()
    };

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeShowTrackingsList = new List<ShowTracking>()
        {
            new()
            {
                UserId = FakeUserId,
                ShowId = FakeShowIds.ElementAt(0),
                EpisodesWatched = 100,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                ShowId = FakeShowIds.ElementAt(1),
                EpisodesWatched = 1,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Subscription
            },
            new()
            {
                UserId = FakeUserId,
                ShowId = FakeShowIds.ElementAt(2),
                EpisodesWatched = 16,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                ShowId = FakeShowIds.ElementAt(3),
                EpisodesWatched = 1,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Planning,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                ShowId = FakeShowIds.ElementAt(4),
                EpisodesWatched = 0,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.Completed,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeDiffUserId,
                ShowId = FakeShowIds.ElementAt(4),
                EpisodesWatched = 1,
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Completed,
                Ownership = MediaTrackingOwnership.Loan
            }
        };

        var fakeShowsList = new List<Show>()
        {
            new() { Id = FakeShowIds.ElementAt(0) },
            new() { Id = FakeShowIds.ElementAt(1) },
            new() { Id = FakeShowIds.ElementAt(2) },
            new() { Id = FakeShowIds.ElementAt(3) },
            new() { Id = FakeShowIds.ElementAt(4) },
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.ShowTrackings.AddRange(fakeShowTrackingsList);
        InMemDatabase.Shows.AddRange(fakeShowsList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetAllShowTrackingsHandler = new GetAllShowTrackingsHandler(InMemDatabase);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task GetAllShowTrackings_Default()
    {
        // Arrange
        var query = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
        };

        // Act
        var result = await GetAllShowTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(5, result.TotalCount);
    }

    [TestMethod]
    public async Task GetAllShowTrackings_ByShowStatus()
    {
        // Arrange
        var queryCompleted = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Completed,
        };
        var queryInProgress = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.InProgress,
        };
        var queryPaused = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Paused,
        };
        var queryPlanning = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Planning,
        };

        // Act
        var resultCompleted = await GetAllShowTrackingsHandler!.Handle(queryCompleted, CancellationToken.None);
        var resultInProgress = await GetAllShowTrackingsHandler.Handle(queryInProgress, CancellationToken.None);
        var resultPaused = await GetAllShowTrackingsHandler.Handle(queryPaused, CancellationToken.None);
        var resultPlanning = await GetAllShowTrackingsHandler.Handle(queryPlanning, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, resultCompleted.TotalCount);
        Assert.AreEqual(2, resultInProgress.TotalCount);
        Assert.AreEqual(1, resultPaused.TotalCount);
        Assert.AreEqual(1, resultPlanning.TotalCount);
    }
    
    [TestMethod]
    public async Task GetAllShowTrackings_SortByFormat()
    {
        // Arrange
        var query = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            SortByFormat = true
        };

        // Act
        var result = await GetAllShowTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(MediaTrackingFormat.Digital, result.Items.First().Format);
        Assert.AreEqual(MediaTrackingFormat.Physical, result.Items.Last().Format);
    }

    [TestMethod]
    public async Task GetAllShowTrackings_SortByOwnership()
    {
        // Arrange
        var query = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            SortByOwnership = true
        };

        // Act
        var result = await GetAllShowTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(MediaTrackingOwnership.Owned, result.Items.First().Ownership);
        Assert.AreEqual(MediaTrackingOwnership.Subscription, result.Items.Last().Ownership);
    }

    [TestMethod]
    public async Task GetAllShowTrackings_SortByEpisodesWatched()
    {
        // Arrange
        var query = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            SortByEpisodesWatched = true
        };

        // Act
        var result = await GetAllShowTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(0, result.Items.First().EpisodesWatched);
        Assert.AreEqual(100, result.Items.Last().EpisodesWatched);
    }

    [TestMethod]
    public async Task GetAllShowTrackings_SortByRecentlyModified()
    {
        // This test modifies the database and should be last to run.
        
        // Arrange
        var query = new GetAllShowTrackingsQuery
        {
            UserId = FakeUserId,
            SortByRecentlyModified = true
        };
        
        var recentlyModifiedShowIdList = new List<Guid>();
        var showTrackings = await InMemDatabase!.ShowTrackings
            .Where(st => st.UserId == FakeUserId)
            .ToListAsync();
        foreach (var showTracking in showTrackings)
        {
            recentlyModifiedShowIdList.Add(showTracking.ShowId);
            showTracking.EpisodesWatched += 1;
            InMemDatabase.Update(showTracking);
            await InMemDatabase.SaveChangesAsync();
            await Task.Delay(1000);
        }

        // Act
        var result = await GetAllShowTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(recentlyModifiedShowIdList.Last(), result.Items.First().ShowId);
        Assert.AreEqual(recentlyModifiedShowIdList.First(), result.Items.Last().ShowId);
    }
}