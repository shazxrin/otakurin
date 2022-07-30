using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Wishlist;
using Otakurin.Core.Exceptions;
using Otakurin.Domain.Wishlist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Wishlist;

[TestClass]
public class RemoveBookWishlistTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static RemoveBookWishlistHandler? RemoveBookWishlistHandler { get; set; }

    private static readonly Guid FakeExistingBookId = Guid.NewGuid();
    private static readonly Guid FakeExistingUserId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBookWishlistsList = new List<BookWishlist>()
        {
            new()
            {
                UserId = FakeExistingUserId,
                BookId = FakeExistingBookId
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
        InMemDatabase.BookWishlists.AddRange(fakeBookWishlistsList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        RemoveBookWishlistHandler = new RemoveBookWishlistHandler(InMemDatabase);
    }

    [TestMethod]
    public async Task RemoveBookWishlist_Exists()
    {
        // Arrange
        var command = new RemoveBookWishlistCommand
        {
            UserId = FakeExistingUserId, 
            BookId = FakeExistingBookId
        };
        
        // Act
        await RemoveBookWishlistHandler!.Handle(command, CancellationToken.None);

        // Assert
        var count = await InMemDatabase!.BookWishlists
            .Where(b => b.UserId.Equals(FakeExistingUserId) 
                        && b.BookId.Equals(FakeExistingBookId))
            .CountAsync();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task RemoveBookWishlist_NotExists()
    {
        // Arrange
        var command = new RemoveBookWishlistCommand 
        { 
            UserId = FakeExistingUserId, 
            BookId = FakeExistingBookId
        };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => 
            RemoveBookWishlistHandler!.Handle(command, CancellationToken.None));
    }
}