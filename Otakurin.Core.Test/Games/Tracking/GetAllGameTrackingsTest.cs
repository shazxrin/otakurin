using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Tracking;

[TestClass]
public class GetAllGameTrackingsTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetAllGameTrackingsHandler? GetAllGameTrackingsHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeDiffUserId = Guid.NewGuid();
    private static readonly List<Guid> FakeGameIds = new ()
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
        var fakeGameTrackingsList = new List<GameTracking>()
        {
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(0),
                HoursPlayed = 100,
                Platform = "PSP",
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(1),
                HoursPlayed = 90,
                Platform = "PC",
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Subscription
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(2),
                HoursPlayed = 80,
                Platform = "XONE",
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(3),
                HoursPlayed = 70,
                Platform = "Switch",
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Planning,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(4),
                HoursPlayed = 60,
                Platform = "PC",
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.Planning,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(5),
                HoursPlayed = 50,
                Platform = "PS4",
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Completed,
                Ownership = MediaTrackingOwnership.Loan
            },
            new()
            {
                UserId = FakeDiffUserId,
                GameId = FakeGameIds.ElementAt(5),
                HoursPlayed = 25,
                Platform = "PC",
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Loan
            }
        };

        var fakeGamesList = new List<Game>()
        {
            new() { Id = FakeGameIds.ElementAt(0) },
            new() { Id = FakeGameIds.ElementAt(1) },
            new() { Id = FakeGameIds.ElementAt(2) },
            new() { Id = FakeGameIds.ElementAt(3) },
            new() { Id = FakeGameIds.ElementAt(4) },
            new() { Id = FakeGameIds.ElementAt(5) }
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.GameTrackings.AddRange(fakeGameTrackingsList);
        InMemDatabase.Games.AddRange(fakeGamesList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetAllGameTrackingsHandler = new GetAllGameTrackingsHandler(InMemDatabase);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }


    [TestMethod]
    public async Task GetAllGameTrackings_Default()
    {
        // Arrange
        var query = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
        };

        // Act
        var result = await GetAllGameTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
    }

    [TestMethod]
    public async Task GetAllGameTrackings_ByGameStatus()
    {
        // Arrange
        var queryCompleted = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Completed,
        };
        var queryInProgress = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.InProgress,
        };
        var queryPaused = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Paused,
        };
        var queryPlanning = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            Status = MediaTrackingStatus.Planning,
        };

        // Act
        var resultCompleted = await GetAllGameTrackingsHandler!.Handle(queryCompleted, CancellationToken.None);
        var resultInProgress = await GetAllGameTrackingsHandler.Handle(queryInProgress, CancellationToken.None);
        var resultPaused = await GetAllGameTrackingsHandler.Handle(queryPaused, CancellationToken.None);
        var resultPlanning = await GetAllGameTrackingsHandler.Handle(queryPlanning, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, resultCompleted.TotalCount);
        Assert.AreEqual(2, resultInProgress.TotalCount);
        Assert.AreEqual(1, resultPaused.TotalCount);
        Assert.AreEqual(2, resultPlanning.TotalCount);
    }

    [TestMethod]
    public async Task GetAllGameTrackings_SortByHoursPlayed()
    {
        // Arrange
        var query = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            SortByHoursPlayed = true
        };

        // Act
        var result = await GetAllGameTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(50, result.Items.First().HoursPlayed);
        Assert.AreEqual(100, result.Items.Last().HoursPlayed);
    }

    [TestMethod]
    public async Task GetAllGameTrackings_SortByPlatform()
    {
        // Arrange
        var query = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            SortByPlatform = true
        };

        // Act
        var result = await GetAllGameTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual("PC", result.Items.First().Platform);
        Assert.AreEqual("XONE", result.Items.Last().Platform);
    }

    [TestMethod]
    public async Task GetAllGameTrackings_SortByFormat()
    {
        // Arrange
        var query = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            SortByFormat = true
        };

        // Act
        var result = await GetAllGameTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(MediaTrackingFormat.Digital, result.Items.First().Format);
        Assert.AreEqual(MediaTrackingFormat.Physical, result.Items.Last().Format);
    }

    [TestMethod]
    public async Task GetAllGameTrackings_SortByOwnership()
    {
        // Arrange
        var query = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            SortByOwnership = true
        };

        // Act
        var result = await GetAllGameTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(MediaTrackingOwnership.Owned, result.Items.First().Ownership);
        Assert.AreEqual(MediaTrackingOwnership.Subscription, result.Items.Last().Ownership);
    }
    
    [TestMethod]
    public async Task GetAllGameTrackings_SortByRecentlyModified()
    {
        // This test modifies the database and should be last to run.
        
        // Arrange
        var query = new GetAllGameTrackingsQuery
        {
            UserId = FakeUserId,
            SortByRecentlyModified = true
        };
        
        var recentlyModifiedGameIdList = new List<Guid>();
        var gameTrackings = await InMemDatabase!.GameTrackings
            .Where(gt => gt.UserId == FakeUserId)
            .ToListAsync();
        foreach (var gameTracking in gameTrackings)
        {
            recentlyModifiedGameIdList.Add(gameTracking.GameId);
            gameTracking.HoursPlayed += 1;
            InMemDatabase.Update(gameTracking);
            await InMemDatabase.SaveChangesAsync();
            await Task.Delay(1000);
        }

        // Act
        var result = await GetAllGameTrackingsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual(recentlyModifiedGameIdList.Last(), result.Items.First().GameId);
        Assert.AreEqual(recentlyModifiedGameIdList.First(), result.Items.Last().GameId);
    }
}