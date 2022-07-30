using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Tracking;
using Otakurin.Core.Exceptions;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Tracking;

[TestClass]
public class RemoveGameTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static RemoveGameTrackingHandler? RemoveGameTrackingHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeGameId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeGame = new Game()
        {
            Id = FakeGameId
        };
        
        var fakeGameTrackingsList = new List<GameTracking>()
        {
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameId,
                HoursPlayed = 100,
                Platform = "PC",
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.Paused,
                Ownership = MediaTrackingOwnership.Owned
            }
        };

        // Setup in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.GameTrackings.AddRange(fakeGameTrackingsList);
        InMemDatabase.Games.Add(fakeGame);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        RemoveGameTrackingHandler = new RemoveGameTrackingHandler(InMemDatabase);
    }

    [TestMethod]
    public async Task RemoveGameTracking_Exists()
    {
        // Arrange
        var command = new RemoveGameTrackingCommand
        {
            UserId = FakeUserId, 
            GameId = FakeGameId, 
            Platform = "PC"
        };
        
        // Act
        await RemoveGameTrackingHandler!.Handle(command, CancellationToken.None);

        // Assert
        var count = await InMemDatabase!.GameTrackings
            .Where(b => b.UserId.Equals(FakeUserId) 
                        && b.GameId.Equals(FakeGameId))
            .CountAsync();
        Assert.AreEqual(0, count);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(FakeUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Game, activity.MediaType);
        Assert.AreEqual(ActivityAction.RemoveTracking, activity.Action);
    }

    [TestMethod]
    public async Task RemoveGameTracking_NotExists()
    {
        // Arrange
        var command = new RemoveGameTrackingCommand 
        { 
            UserId = FakeUserId, 
            GameId = FakeGameId, 
            Platform = "PS4"
        };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => 
            RemoveGameTrackingHandler!.Handle(command, CancellationToken.None));
    }
}