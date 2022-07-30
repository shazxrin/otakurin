using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Wishlist;
using Otakurin.Domain.Media;
using Otakurin.Domain.Wishlist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Books.Wishlist;

[TestClass]
public class GetAllBookWishlistsTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetAllBookWishlistsHandler? GetAllBookWishlistsHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeDiffUserId = Guid.NewGuid();
    private static readonly List<Guid> FakeBookIds = new ()
    {
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid()
    };

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBookWishlistsList = new List<BookWishlist>()
        {
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(0)
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(1)
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(2)
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(3)
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(4)
            },
            new()
            {
                UserId = FakeUserId,
                BookId = FakeBookIds.ElementAt(5)
            },
            new()
            {
                UserId = FakeDiffUserId,
                BookId = FakeBookIds.ElementAt(5)
            }
        };

        var fakeBooksList = new List<Book>()
        {
            new() { Id = FakeBookIds.ElementAt(0) },
            new() { Id = FakeBookIds.ElementAt(1) },
            new() { Id = FakeBookIds.ElementAt(2) },
            new() { Id = FakeBookIds.ElementAt(3) },
            new() { Id = FakeBookIds.ElementAt(4) },
            new() { Id = FakeBookIds.ElementAt(5) },
        };

        // Setup in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.BookWishlists.AddRange(fakeBookWishlistsList);
        InMemDatabase.Books.AddRange(fakeBooksList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetAllBookWishlistsHandler = new GetAllBookWishlistsHandler(InMemDatabase);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }


    [TestMethod]
    public async Task GetAllBookWishlists_Default()
    {
        // Setup
        var query = new GetAllBookWishlistsQuery
        {
            UserId = FakeUserId,
        };

        // Execute
        var result = await GetAllBookWishlistsHandler!.Handle(query, CancellationToken.None);

        // Verify
        Assert.AreEqual(6, result.TotalCount);
    }
}