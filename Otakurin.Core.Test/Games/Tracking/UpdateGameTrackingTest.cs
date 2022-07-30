using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Tracking;

[TestClass]
public class UpdateGameTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static UpdateGameTrackingHandler? UpdateGameTrackingHandler { get; set; }

    private static IMapper? Mapper { get; set; }

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        UpdateGameTrackingHandler = new UpdateGameTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task UpdateGameTracking_Exists()
    {
        // Arrange
        var fakeUserId = Guid.NewGuid();
        var fakeGameId = Guid.NewGuid();
        var fakeHoursPlayed = 10;
        var fakePlatform = "PC";
        var fakeFormat = MediaTrackingFormat.Digital;
        var fakeStatus = MediaTrackingStatus.Planning;
        var fakeOwnership = MediaTrackingOwnership.Subscription;
        InMemDatabase!.GameTrackings.Add(new GameTracking
        {
            UserId = fakeUserId,
            GameId = fakeGameId,
            HoursPlayed = fakeHoursPlayed,
            Platform = fakePlatform,
            Format = fakeFormat,
            Status = fakeStatus,
            Ownership = fakeOwnership
        });
        InMemDatabase.Games.Add(new Game
        {
            Id = fakeGameId
        });
        await InMemDatabase.SaveChangesAsync(CancellationToken.None);

        var newFakeHoursPlayed = 25;
        var newFakeFormat = MediaTrackingFormat.Physical;
        var newFakeStatus = MediaTrackingStatus.InProgress;
        var newFakeOwnership = MediaTrackingOwnership.Owned;
        var command = new UpdateGameTrackingCommand
        {
            UserId = fakeUserId, 
            GameId = fakeGameId, 
            Platform = fakePlatform, 
            HoursPlayed = newFakeHoursPlayed,
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        
        // Act
         await UpdateGameTrackingHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedGameTracking = await InMemDatabase.GameTrackings
            .AsNoTracking()
            .Where(tg => tg.UserId == fakeUserId && tg.GameId == fakeGameId)
            .FirstOrDefaultAsync(CancellationToken.None);
        Assert.IsNotNull(updatedGameTracking);
        Assert.AreEqual(updatedGameTracking.HoursPlayed, newFakeHoursPlayed);
        Assert.AreEqual(updatedGameTracking.Platform, fakePlatform);
        Assert.AreEqual(updatedGameTracking.Format, newFakeFormat);
        Assert.AreEqual(updatedGameTracking.Status, newFakeStatus);
        Assert.AreEqual(updatedGameTracking.Ownership, newFakeOwnership);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(fakeUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Game, activity.MediaType);
        Assert.AreEqual(ActivityAction.UpdateTracking, activity.Action);
    }

    [TestMethod]
    public async Task UpdateGameTracking_NotExists()
    {
        // Arrange
        var fakeUserId = Guid.NewGuid();
        var fakeDiffUserId = Guid.NewGuid();
        var fakeGameId = Guid.NewGuid();
        var fakeDiffGameId = Guid.NewGuid();
        var fakeHoursPlayed = 10;
        var fakePlatform = "PC";
        var fakeDiffPlatform = "Switch";
        var fakeFormat = MediaTrackingFormat.Digital;
        var fakeStatus = MediaTrackingStatus.Planning;
        var fakeOwnership = MediaTrackingOwnership.Subscription;
        InMemDatabase!.GameTrackings.Add(new GameTracking
        {
            UserId = fakeUserId,
            GameId = fakeGameId,
            HoursPlayed = fakeHoursPlayed,
            Platform = fakePlatform,
            Format = fakeFormat,
            Status = fakeStatus,
            Ownership = fakeOwnership
        });
        InMemDatabase.Games.Add(new Game
        {
            Id = fakeGameId
        });
        await InMemDatabase.SaveChangesAsync(CancellationToken.None);
        
        var newFakeHoursPlayed = 25;
        var newFakeFormat = MediaTrackingFormat.Physical;
        var newFakeStatus = MediaTrackingStatus.InProgress;
        var newFakeOwnership = MediaTrackingOwnership.Owned;
        
        var commandDiffUser = new UpdateGameTrackingCommand
        {
            UserId = fakeDiffUserId, 
            GameId = fakeGameId, 
            Platform = fakePlatform,
            HoursPlayed = newFakeHoursPlayed, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffGame = new UpdateGameTrackingCommand
        {
            UserId = fakeUserId, 
            GameId = fakeDiffGameId, 
            Platform = fakePlatform,
            HoursPlayed = newFakeHoursPlayed, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffPlatform = new UpdateGameTrackingCommand
        {
            UserId = fakeUserId, 
            GameId = fakeGameId, 
            Platform = fakeDiffPlatform,
            HoursPlayed = newFakeHoursPlayed, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffUserAndGame = new UpdateGameTrackingCommand
        {
            UserId = fakeDiffUserId, 
            GameId = fakeDiffGameId,
            Platform = fakePlatform,
            HoursPlayed = newFakeHoursPlayed, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffGameAndPlatform = new UpdateGameTrackingCommand
        {
            UserId = fakeUserId, 
            GameId = fakeDiffGameId, 
            Platform = fakeDiffPlatform,
            HoursPlayed = newFakeHoursPlayed, 
            Format = newFakeFormat, 
            Status = newFakeStatus,
            Ownership = newFakeOwnership
        };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateGameTrackingHandler!.Handle(commandDiffUser, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateGameTrackingHandler!.Handle(commandDiffGame, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateGameTrackingHandler!.Handle(commandDiffPlatform, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateGameTrackingHandler!.Handle(commandDiffUserAndGame, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateGameTrackingHandler!.Handle(commandDiffGameAndPlatform, CancellationToken.None));
    }
}