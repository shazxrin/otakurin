using System;
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
public class AddGameTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static AddGameTrackingHandler? AddGameTrackingHandler { get; set; }

    private static readonly Guid FakeExistingGameId = Guid.NewGuid();
    private static readonly Guid FakeExistingUserId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeUser = new UserAccount()
        {
            Id = FakeExistingUserId
        };

        var fakeGame = new Game()
        {
            Id = FakeExistingGameId
        };

        // Setup in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();

        InMemDatabase.Games.Add(fakeGame);
        InMemDatabase.Users.Add(fakeUser);

        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        AddGameTrackingHandler = new AddGameTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task AddGameTracking_Default()
    {
        // Setup
        var command = new AddGameTrackingCommand
        {
            UserId = FakeExistingUserId,
            GameId = FakeExistingGameId,
            HoursPlayed = 200,
            Platform = "PC",
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Execute
        await AddGameTrackingHandler!.Handle(command, CancellationToken.None);

        // Verify
        var gameTracking = await InMemDatabase!.GameTrackings
            .Where(gt => gt.GameId.Equals(FakeExistingGameId)
                         && gt.UserId.Equals(FakeExistingUserId))
            .CountAsync();
        Assert.AreEqual(1, gameTracking);

        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(FakeExistingUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Game, activity.MediaType);
        Assert.AreEqual(ActivityAction.AddTracking, activity.Action);
    }

    [TestMethod]
    public async Task AddGameTracking_TrackingExists()
    {
        // Setup
        var command = new AddGameTrackingCommand
        {
            UserId = FakeExistingUserId,
            GameId = FakeExistingGameId,
            HoursPlayed = 200,
            Platform = "PC",
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };

    // Execute & Verify
        await Assert.ThrowsExceptionAsync<ExistsException>(() =>
            AddGameTrackingHandler!.Handle(command, CancellationToken.None));
    }

    [TestMethod]
    public async Task AddGameTracking_GameNotFound()
    {
        // Setup
        var command = new AddGameTrackingCommand
        {
            UserId = FakeExistingUserId,
            GameId = Guid.NewGuid(),
            HoursPlayed = 200,
            Platform = "PC",
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Execute & Verify
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            AddGameTrackingHandler!.Handle(command, CancellationToken.None));
    }

    [TestMethod]
    public async Task AddBookTracking_UserNotFound()
    {
        var command = new AddGameTrackingCommand
        {
            UserId = Guid.NewGuid(),
            GameId = FakeExistingGameId,
            HoursPlayed = 200,
            Platform = "PC",
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Execute & Verify
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            AddGameTrackingHandler!.Handle(command, CancellationToken.None));
    }
}