using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Wishlist;
using Otakurin.Domain.Wishlist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Wishlist;

[TestClass]
public class GetBookWishlistTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetBookWishlistHandler? GetBookWishlistHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeBookId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBookWishlistsList = new List<BookWishlist>()
        {
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookId
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

        GetBookWishlistHandler = new GetBookWishlistHandler(InMemDatabase);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task GetBookWishlist_Found()
    {
        // Arrange
        var query = new GetBookWishlistQuery 
        { 
            UserId = FakeUserId, 
            BookId = FakeBookId
        };

        // Act
        var result = await GetBookWishlistHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public async Task GetBookWishlist_NotFound()
    {
        // Arrange
        var query = new GetBookWishlistQuery
        {
            UserId = FakeUserId, 
            BookId = Guid.NewGuid()
        };

        // Act
        var result = await GetBookWishlistHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.IsFalse(result);
    }
}