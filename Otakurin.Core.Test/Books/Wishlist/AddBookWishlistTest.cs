using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Wishlist;
using Otakurin.Core.Exceptions;
using Otakurin.Domain.Media;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Wishlist;

[TestClass]
public class AddBookWishlistTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static IMapper? Mapper { get; set; }
    
    private static AddBookWishlistHandler? AddBookWishlistHandler { get; set; }

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

        AddBookWishlistHandler = new AddBookWishlistHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task AddBookWishlist_Default()
    {
        // Arrange
        var command = new AddBookWishlistCommand
        {
            UserId = FakeExistingUserId,
            BookId = FakeExistingBookId
        };
        
        // Act
        await AddBookWishlistHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var bookWishlist = await InMemDatabase!.BookWishlists
            .Where(bt => bt.BookId.Equals(FakeExistingBookId) 
                         && bt.UserId.Equals(FakeExistingUserId))
            .CountAsync();
        Assert.AreEqual(1, bookWishlist);
    }

    [TestMethod]
    public async Task AddBookWishlist_WishlistExists()
    {
        // Arrange
        var command = new AddBookWishlistCommand
        {
            UserId = FakeExistingUserId,
            BookId = FakeExistingBookId
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ExistsException>(() => AddBookWishlistHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddBookWishlist_BookNotFound()
    {
        // Arrange
        var command = new AddBookWishlistCommand
        {
            UserId = FakeExistingUserId,
            BookId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddBookWishlistHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddBookWishlist_UserNotFound()
    {
        // Arrange
        var command = new AddBookWishlistCommand
        {
            UserId = Guid.NewGuid(),
            BookId = FakeExistingBookId
        };
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddBookWishlistHandler!.Handle(command, CancellationToken.None));
    }
}