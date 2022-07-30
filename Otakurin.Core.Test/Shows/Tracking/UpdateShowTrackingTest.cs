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
public class UpdateShowTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static UpdateShowTrackingHandler? UpdateShowTrackingHandler { get; set; }

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

        UpdateShowTrackingHandler = new UpdateShowTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task UpdateShowTracking_Exists()
    {
        // Arrange
        var fakeUserId = Guid.NewGuid();
        var fakeShowId = Guid.NewGuid();
        var fakeEpisodesWatched = 10;
        var fakeFormat = MediaTrackingFormat.Physical;
        var fakeStatus = MediaTrackingStatus.InProgress;
        var fakeOwnership = MediaTrackingOwnership.Loan;
        var fakeShowType = ShowType.Series;
        InMemDatabase!.ShowTrackings.Add(new ShowTracking
        {
            UserId = fakeUserId,
            ShowId = fakeShowId,
            EpisodesWatched = fakeEpisodesWatched,
            Format = fakeFormat,
            Status = fakeStatus,
            Ownership = fakeOwnership
        });
        InMemDatabase.Shows.Add(new Show()
        {
            Id = fakeShowId
        });
        await InMemDatabase.SaveChangesAsync(CancellationToken.None);

        // Simulated Update
        var newFakeEpisodesWatched = 16;
        var newFakeFormat = MediaTrackingFormat.Digital;
        var newFakeStatus = MediaTrackingStatus.Completed;
        var newFakeOwnership = MediaTrackingOwnership.Subscription;

        var command = new UpdateShowTrackingCommand
        {
            UserId = fakeUserId, 
            ShowId = fakeShowId, 
            EpisodesWatched = newFakeEpisodesWatched, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        
        // Act
         await UpdateShowTrackingHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedShowTracking = await InMemDatabase.ShowTrackings
            .AsNoTracking()
            .Where(showTracking => showTracking.UserId == fakeUserId && showTracking.ShowId == fakeShowId)
            .FirstOrDefaultAsync(CancellationToken.None);
        Assert.IsNotNull(updatedShowTracking);
        Assert.AreEqual(updatedShowTracking.EpisodesWatched, newFakeEpisodesWatched);
        Assert.AreEqual(updatedShowTracking.Status, newFakeStatus);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(fakeUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Show, activity.MediaType);
        Assert.AreEqual(ActivityAction.UpdateTracking, activity.Action);
    }

    [TestMethod]
    public async Task UpdateShowTracking_NotExists()
    {
        // Arrange
        var fakeUserId = Guid.NewGuid();
        var fakeDiffUserId = Guid.NewGuid();
        var fakeShowId = Guid.NewGuid();
        var fakeDiffShowId = Guid.NewGuid();
        var fakeEpisodesWatched = 10;
        var fakeFormat = MediaTrackingFormat.Physical;
        var fakeStatus = MediaTrackingStatus.InProgress;
        var fakeOwnership = MediaTrackingOwnership.Loan;
        var fakeShowType = ShowType.Series;
        
        InMemDatabase!.ShowTrackings.Add(new ShowTracking
        {
            UserId = fakeUserId,
            ShowId = fakeShowId,
            EpisodesWatched = fakeEpisodesWatched,
            Format = fakeFormat,
            Status = fakeStatus,
            Ownership = fakeOwnership
        });
        InMemDatabase.Shows.Add(new Show()
        {
            Id = fakeShowId
        });
        await InMemDatabase.SaveChangesAsync(CancellationToken.None);
        
        var newFakeEpisodesWatched = 25;
        var newFakeFormat = MediaTrackingFormat.Digital;
        var newFakeStatus = MediaTrackingStatus.Completed;
        var newFakeOwnership = MediaTrackingOwnership.Subscription;
        
        var commandDiffUser = new UpdateShowTrackingCommand
        {
            UserId = fakeDiffUserId, 
            ShowId = fakeShowId, 
            EpisodesWatched = newFakeEpisodesWatched, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffShow = new UpdateShowTrackingCommand
        { 
            UserId = fakeUserId, 
            ShowId = fakeDiffShowId, 
            EpisodesWatched = newFakeEpisodesWatched, 
            Format = newFakeFormat,
            Status = newFakeStatus,
            Ownership = newFakeOwnership
        };
        var commandDiffUserAndShow = new UpdateShowTrackingCommand 
        {
            UserId = fakeDiffUserId,
            ShowId = fakeDiffShowId,
            EpisodesWatched = newFakeEpisodesWatched,
            Format = newFakeFormat,
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateShowTrackingHandler!.Handle(commandDiffUser, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateShowTrackingHandler!.Handle(commandDiffShow, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateShowTrackingHandler!.Handle(commandDiffUserAndShow, CancellationToken.None));
    }
}