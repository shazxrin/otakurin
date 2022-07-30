using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Tracking;
using Otakurin.Core.Exceptions;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Tracking;

[TestClass]
public class RemoveBookTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static RemoveBookTrackingHandler? RemoveBookTrackingHandler { get; set; }

    private static readonly Guid FakeExistingUserId = Guid.NewGuid();
    private static readonly Guid FakeExistingBookId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBook = new Book()
        {
            Id = FakeExistingBookId
        };
        
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
        InMemDatabase.Books.Add(fakeBook);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        RemoveBookTrackingHandler = new RemoveBookTrackingHandler(InMemDatabase);
    }

    [TestMethod]
    public async Task RemoveBookTracking_Exists()
    {
        // Arrange
        var command = new RemoveBookTrackingCommand 
        {
            UserId = FakeExistingUserId, 
            BookId = FakeExistingBookId
        };
        
        // Act
        await RemoveBookTrackingHandler!.Handle(command, CancellationToken.None);

        // Arrange
        var count = await InMemDatabase!.BookTrackings
            .Where(b => b.UserId.Equals(FakeExistingUserId) 
                        && b.BookId.Equals(FakeExistingBookId))
            .CountAsync();
        Assert.AreEqual(0, count);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(FakeExistingUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Book, activity.MediaType);
        Assert.AreEqual(ActivityAction.RemoveTracking, activity.Action);
    }

    [TestMethod]
    public async Task RemoveBookTracking_NotExists()
    {
        // Arrange
        var command = new RemoveBookTrackingCommand 
        {
            UserId = FakeExistingUserId, 
            BookId = FakeExistingBookId
        };

        // Act
        // Arrange
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => 
            RemoveBookTrackingHandler!.Handle(command, CancellationToken.None));
    }
}