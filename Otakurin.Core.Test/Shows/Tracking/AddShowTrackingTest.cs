using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Shows.Tracking;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Shows.Tracking;

[TestClass]
public class AddShowTrackingTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static AddShowTrackingHandler? AddShowTrackingHandler { get; set; }

    private static readonly Guid FakeShowId = Guid.NewGuid();
    private static readonly Guid FakeUserId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeUser = new UserAccount()
        {
            Id = FakeUserId
        };

        var fakeShow = new Show()
        {
            Id = FakeShowId
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();

        InMemDatabase.Shows.Add(fakeShow);
        InMemDatabase.Users.Add(fakeUser);

        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        AddShowTrackingHandler = new AddShowTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task AddShowTracking_Default()
    {
        // Arrange
        var command = new AddShowTrackingCommand
        {
            UserId = FakeUserId,
            ShowId = FakeShowId,
            EpisodesWatched = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };
        
        // Act
        await AddShowTrackingHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var showTracking = await InMemDatabase!.ShowTrackings
            .Where(showTracking => showTracking.ShowId.Equals(FakeShowId) 
                                   && showTracking.UserId.Equals(FakeUserId))
            .CountAsync();
        Assert.AreEqual(1, showTracking);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(FakeUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Show, activity.MediaType);
        Assert.AreEqual(ActivityAction.AddTracking, activity.Action);
    }

    [TestMethod]
    public async Task AddShowTracking_TrackingExists()
    {
        // Arrange
        var command = new AddShowTrackingCommand
        {
            UserId = FakeUserId,
            ShowId = FakeShowId,
            EpisodesWatched = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ExistsException>(() => AddShowTrackingHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddShowTracking_ShowNotFound()
    {
        // Arrange
        var command = new AddShowTrackingCommand
        {
            UserId = FakeUserId,
            ShowId = Guid.NewGuid(),
            EpisodesWatched = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddShowTrackingHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddShowTracking_UserNotFound()
    {
        var command = new AddShowTrackingCommand
        {
            UserId = Guid.NewGuid(),
            ShowId = FakeShowId,
            EpisodesWatched = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Completed,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddShowTrackingHandler!.Handle(command, CancellationToken.None));
    }
}