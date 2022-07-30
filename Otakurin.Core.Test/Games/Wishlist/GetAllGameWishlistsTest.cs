using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Wishlist;
using Otakurin.Domain.Media;
using Otakurin.Domain.Wishlist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Wishlist;

[TestClass]
public class GetAllGameWishlistsTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetAllGameWishlistsHandler? GetAllGameWishlistsHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeDiffUserId = Guid.NewGuid();
    private static readonly List<Guid> FakeGameIds = new ()
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
        var fakeGameWishlistsList = new List<GameWishlist>()
        {
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(0),
                Platform = "PC"
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(1),
                Platform = "PC"
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(2),
                Platform = "Switch"
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(3),
                Platform = "PC"
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(4),
                Platform = "PC"
            },
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameIds.ElementAt(5),
                Platform = "PS5"
            },
            new()
            {
                UserId = FakeDiffUserId,
                GameId = FakeGameIds.ElementAt(5),
                Platform = "PS5"
            }
        };

        var fakeGamesList = new List<Game>()
        {
            new() { Id = FakeGameIds.ElementAt(0) },
            new() { Id = FakeGameIds.ElementAt(1) },
            new() { Id = FakeGameIds.ElementAt(2) },
            new() { Id = FakeGameIds.ElementAt(3) },
            new() { Id = FakeGameIds.ElementAt(4) },
            new() { Id = FakeGameIds.ElementAt(5) },
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.GameWishlists.AddRange(fakeGameWishlistsList);
        InMemDatabase.Games.AddRange(fakeGamesList);
        await InMemDatabase.SaveChangesAsync();
        
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetAllGameWishlistsHandler = new GetAllGameWishlistsHandler(InMemDatabase);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }


    [TestMethod]
    public async Task GetAllGameWishlists_Default()
    {
        // Arrange
        var query = new GetAllGameWishlistsQuery
        {
            UserId = FakeUserId,
        };

        // Act
        var result = await GetAllGameWishlistsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
    }

    [TestMethod]
    public async Task GetAllGameWishlists_SortByPlatform()
    {
        // Arrange
        var query = new GetAllGameWishlistsQuery
        {
            UserId = FakeUserId,
            SortByPlatform = true
        };

        // Act
        var result = await GetAllGameWishlistsHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual(6, result.TotalCount);
        Assert.AreEqual("PC", result.Items.First().Platform);
        Assert.AreEqual("Switch", result.Items.Last().Platform);
    }
}