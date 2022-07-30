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
public class UpdateBookTrackingTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static UpdateBookTrackingHandler? UpdateBookTrackingHandler { get; set; }

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

        UpdateBookTrackingHandler = new UpdateBookTrackingHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task UpdateBookTracking_Exists()
    {
        // Arrange
        var fakeUserId = Guid.NewGuid();
        var fakeBookId = Guid.NewGuid();
        var fakeChaptersRead = 10;
        var fakeFormat = MediaTrackingFormat.Digital;
        var fakeStatus = MediaTrackingStatus.Planning;
        var fakeOwnership = MediaTrackingOwnership.Owned;
        InMemDatabase!.BookTrackings.Add(new BookTracking
        {
            UserId = fakeUserId,
            BookId = fakeBookId,
            ChaptersRead = fakeChaptersRead,
            Format = fakeFormat,
            Status = fakeStatus,
            Ownership = fakeOwnership
        });
        InMemDatabase.Books.Add(new Book
        {
            Id = fakeBookId
        });
        await InMemDatabase.SaveChangesAsync(CancellationToken.None);

        var newFakeChaptersRead = 25;
        var newFakeFormat = MediaTrackingFormat.Physical;
        var newFakeStatus = MediaTrackingStatus.InProgress;
        var newFakeOwnership = MediaTrackingOwnership.Loan;
        var command = new UpdateBookTrackingCommand
        {
            UserId = fakeUserId, 
            BookId = fakeBookId, 
            ChaptersRead = newFakeChaptersRead,
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        
        // Act
         await UpdateBookTrackingHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedBookTracking = await InMemDatabase.BookTrackings
            .AsNoTracking()
            .Where(bt => bt.UserId == fakeUserId && bt.BookId == fakeBookId)
            .FirstOrDefaultAsync(CancellationToken.None);
        Assert.IsNotNull(updatedBookTracking);
        Assert.AreEqual(updatedBookTracking.ChaptersRead, newFakeChaptersRead);
        Assert.AreEqual(updatedBookTracking.Format, newFakeFormat);
        Assert.AreEqual(updatedBookTracking.Status, newFakeStatus);
        Assert.AreEqual(updatedBookTracking.Ownership, newFakeOwnership);
        
        var activity = await InMemDatabase.Activities
            .Where(a => a.UserId.Equals(fakeUserId))
            .FirstOrDefaultAsync();
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityMediaType.Book, activity.MediaType);
        Assert.AreEqual(ActivityAction.UpdateTracking, activity.Action);
    }

    [TestMethod]
    public async Task UpdateBookTracking_NotExists()
    {
        // Arrange
        var fakeUserId = Guid.NewGuid();
        var fakeDiffUserId = Guid.NewGuid();
        var fakeBookId = Guid.NewGuid();
        var fakeDiffBookId = Guid.NewGuid();
        var fakeChaptersRead = 10;
        var fakeFormat = MediaTrackingFormat.Digital;
        var fakeStatus = MediaTrackingStatus.Planning;
        var fakeOwnership = MediaTrackingOwnership.Owned;
        var newFakeChaptersRead = 25;
        var newFakeFormat = MediaTrackingFormat.Physical;
        var newFakeStatus = MediaTrackingStatus.InProgress;
        var newFakeOwnership = MediaTrackingOwnership.Owned;
        
        InMemDatabase!.BookTrackings.Add(new BookTracking
        {
            UserId = fakeUserId,
            BookId = fakeBookId,
            ChaptersRead = fakeChaptersRead,
            Format = fakeFormat,
            Status = fakeStatus,
            Ownership = fakeOwnership
        });
        InMemDatabase.Books.Add(new Book
        {
            Id = fakeBookId
        });
        await InMemDatabase.SaveChangesAsync(CancellationToken.None);
        
        var commandDiffUser = new UpdateBookTrackingCommand
        {
            UserId = fakeDiffUserId, 
            BookId = fakeBookId,
            ChaptersRead = newFakeChaptersRead, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffBook = new UpdateBookTrackingCommand
        {
            UserId = fakeUserId, 
            BookId = fakeDiffBookId,
            ChaptersRead = newFakeChaptersRead, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };
        var commandDiffUserAndBook = new UpdateBookTrackingCommand
        {
            UserId = fakeDiffUserId, 
            BookId = fakeDiffBookId,
            ChaptersRead = newFakeChaptersRead, 
            Format = newFakeFormat, 
            Status = newFakeStatus, 
            Ownership = newFakeOwnership
        };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateBookTrackingHandler!.Handle(commandDiffUser, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateBookTrackingHandler!.Handle(commandDiffBook, CancellationToken.None));
        await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
            UpdateBookTrackingHandler!.Handle(commandDiffUserAndBook, CancellationToken.None));
    }
}