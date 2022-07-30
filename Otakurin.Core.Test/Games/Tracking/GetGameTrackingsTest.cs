using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Tracking;

[TestClass]
public class GetGameTrackingsTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetGameTrackingsHandler? GetGameTrackingsHandler { get; set; }
    
    private static readonly Guid FakeExistingUserId = Guid.NewGuid();
    private static readonly Guid FakeExistingGameId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeGameTrackingsList = new List<GameTracking>()
        {
            new()
            {
                UserId = FakeExistingUserId,
                GameId = FakeExistingGameId,
                HoursPlayed = 100,
                Platform = "PSP",
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Owned
            },
            new()
            {
                UserId = FakeExistingUserId,
                GameId = FakeExistingGameId,
                HoursPlayed = 90,
                Platform = "PC",
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
                Ownership = MediaTrackingOwnership.Subscription
            },
            new()
            {
                UserId = FakeExistingUserId,
                GameId = Guid.NewGuid(),
                HoursPlayed = 80,
                Platform = "XONE",
                Format = MediaTrackingFormat.Physical,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Owned
            }
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
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetGameTrackingsHandler = new GetGameTrackingsHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task GetGameTrackings_SameGameDiffPlatforms()
    {
        // Arrange
        var query = new GetGameTrackingsQuery
        {
            UserId = FakeExistingUserId, 
            GameId = FakeExistingGameId
        };

        // Act
        var result = await GetGameTrackingsHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(2, result.Items.Count);
    }
}