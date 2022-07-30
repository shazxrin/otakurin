using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Tracking;
using Otakurin.Domain.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Tracking;

[TestClass]
public class GetBookTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetBookTrackingHandler? GetBookTrackingHandler { get; set; }

    private static readonly Guid FakeExistingUserId = Guid.NewGuid();
    private static readonly Guid FakeExistingBookId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBookTrackingsList = new List<BookTracking>()
        {
            new()
            {
                UserId = FakeExistingUserId,
                BookId = FakeExistingBookId,
                ChaptersRead = 100,
                Format = MediaTrackingFormat.Digital,
                Status = MediaTrackingStatus.InProgress,
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
        InMemDatabase.BookTrackings.AddRange(fakeBookTrackingsList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetBookTrackingHandler = new GetBookTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task GetBookTracking_Found()
    {
        // Arrange
        var query = new GetBookTrackingQuery 
        {
            UserId = FakeExistingUserId, 
            BookId = FakeExistingBookId
        };

        // Act
        var result = await GetBookTrackingHandler!.Handle(query, CancellationToken.None);
        
        // Arrange
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public async Task GetBookTracking_NotFound()
    {
        // Arrange
        var query = new GetBookTrackingQuery 
        {
            UserId = FakeExistingUserId, 
            BookId = Guid.NewGuid()
        };

        // Act
        var result = await GetBookTrackingHandler!.Handle(query, CancellationToken.None);
        
        // Arrange
        Assert.IsNull(result);
    }
}