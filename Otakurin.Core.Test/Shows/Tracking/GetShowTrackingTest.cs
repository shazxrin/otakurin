using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Shows.Tracking;
using Otakurin.Domain.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Shows.Tracking;

[TestClass]
public class GetShowTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetShowTrackingHandler? GetShowTrackingHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeShowId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeShowTrackingList = new List<ShowTracking>()
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
        InMemDatabase.ShowTrackings.AddRange(fakeShowTrackingList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetShowTrackingHandler = new GetShowTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task GetShowTracking_Found()
    {
        // Arrange
        var query = new GetShowTrackingQuery { UserId = FakeUserId, ShowId = FakeShowId };

        // Act
        var result = await GetShowTrackingHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public async Task GetShowTracking_NotFound()
    {
        // Arrange
        var query = new GetShowTrackingQuery { UserId = FakeUserId, ShowId = Guid.NewGuid() };

        // Act
        var result = await GetShowTrackingHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.IsNull(result);
    }
}