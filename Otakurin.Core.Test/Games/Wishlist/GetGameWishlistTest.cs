using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Wishlist;
using Otakurin.Domain;
using Otakurin.Domain.Wishlist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Wishlist;

[TestClass]
public class GetGameWishlistTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static GetGameWishlistsHandler? GetGameWishlistsHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeGameId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeGameWishlistsList = new List<GameWishlist>()
        {
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameId,
                Platform = "PC"
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
        InMemDatabase.GameWishlists.AddRange(fakeGameWishlistsList);
        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetGameWishlistsHandler = new GetGameWishlistsHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }

    [TestMethod]
    public async Task GetGameWishlists_Found()
    {
        // Arrange
        var query = new GetGameWishlistsQuery
        {
            UserId = FakeUserId, 
            GameId = FakeGameId
        };

        // Act
        var result = await GetGameWishlistsHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public async Task GetGameWishlists_NotFound()
    {
        // Arrange
        var query = new GetGameWishlistsQuery
        {
            UserId = FakeUserId, 
            GameId = Guid.NewGuid()
        };

        // Act
        var result = await GetGameWishlistsHandler!.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(0, result.Items.Count);
    }
}