using System;
using System.Collections.Generic;
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
public class RemoveShowTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static RemoveShowTrackingHandler? RemoveShowTrackingHandler { get; set; }

    private static readonly Guid FakeShowId = Guid.NewGuid();
    private static readonly Guid FakeUserId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeShow = new Show()
        {
            Id = FakeShowId
        };
        
        var fakeShowTrackingsList = new List<ShowTracking>()
        {
            new()
            {
                UserId = FakeUserId,
                ShowId = FakeShowId,
                EpisodesWatched = 123,
                Status = MediaTrackingStatus.InProgress
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
        InMemDatabase.ShowTrackings.AddRange(fakeShowTrackingsList);
        InMemDatabase.Shows.Add(fakeShow);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        RemoveShowTrackingHandler = new RemoveShowTrackingHandler(InMemDatabase);
    }

    [TestMethod]
    public async Task RemoveShowTracking_Exists()
    {
        // Arrange
        var command = new RemoveShowTrackingCommand { UserId = FakeUserId, ShowId = FakeShowId };
        
        // Act
        await RemoveShowTrackingHandler!.Handle(command, CancellationToken.None);

        // Assert
        var count = await InMemDatabase!.ShowTrackings
            .Where(st => st.UserId.Equals(FakeUserId)
                                   && st.ShowId.Equals(FakeShowId))
            .CountAsync();
        Assert.AreEqual(0, count);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(FakeUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Show, activity.MediaType);
        Assert.AreEqual(ActivityAction.RemoveTracking, activity.Action);
    }

    [TestMethod]
    public async Task RemoveShowTracking_NotExists()
    {
        // Arrange
        var command = new RemoveShowTrackingCommand { UserId = FakeUserId, ShowId = FakeShowId };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => 
            RemoveShowTrackingHandler!.Handle(command, CancellationToken.None));
    }
}