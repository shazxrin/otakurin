using System;
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
public class AddBookTrackingTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static AddBookTrackingHandler? AddBookTrackingHandler { get; set; }

    private static readonly Guid FakeExistingBookId = Guid.NewGuid();
    private static readonly Guid FakeExistingUserId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeUser = new UserAccount()
        {
            Id = FakeExistingUserId
        };

        var fakeBook = new Book()
        {
            Id = FakeExistingBookId
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();

        InMemDatabase.Books.Add(fakeBook);
        InMemDatabase.Users.Add(fakeUser);

        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        AddBookTrackingHandler = new AddBookTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task AddBookTracking_Default()
    {
        // Arrange
        var command = new AddBookTrackingCommand
        {
            UserId = FakeExistingUserId,
            BookId = FakeExistingBookId,
            ChaptersRead = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Planning,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Act
        await AddBookTrackingHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var bookTracking = await InMemDatabase!.BookTrackings
            .Where(bt => bt.BookId.Equals(FakeExistingBookId) 
                         && bt.UserId.Equals(FakeExistingUserId))
            .CountAsync();
        Assert.AreEqual(1, bookTracking);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(FakeExistingUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Book, activity.MediaType);
        Assert.AreEqual(ActivityAction.AddTracking, activity.Action);
    }

    [TestMethod]
    public async Task AddBookTracking_TrackingExists()
    {
        // Arrange
        var command = new AddBookTrackingCommand
        {
            UserId = FakeExistingUserId,
            BookId = FakeExistingBookId,
            ChaptersRead = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Planning,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ExistsException>(() => AddBookTrackingHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddBookTracking_BookNotFound()
    {
        // Arrange
        var command = new AddBookTrackingCommand
        {
            UserId = FakeExistingUserId,
            BookId = Guid.NewGuid(),
            ChaptersRead = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Planning,
            Ownership = MediaTrackingOwnership.Owned
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddBookTrackingHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddBookTracking_UserNotFound()
    {
        // Arrange
        var command = new AddBookTrackingCommand
        {
            UserId = Guid.NewGuid(),
            BookId = FakeExistingBookId, 
            ChaptersRead = 200,
            Format = MediaTrackingFormat.Digital,
            Status = MediaTrackingStatus.Planning,
            Ownership = MediaTrackingOwnership.Owned
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddBookTrackingHandler!.Handle(command, CancellationToken.None));
    }
}