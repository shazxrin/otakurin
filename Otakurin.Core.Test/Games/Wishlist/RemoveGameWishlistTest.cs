using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Wishlist;
using Otakurin.Domain.Wishlist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Games.Wishlist;

[TestClass]
public class RemoveGameWishlistTest
{
    private static SqliteConnection? Connection { get; set; }

    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }

    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }

    private static RemoveGameWishlistHandler? RemoveGameWishlistHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly string FakeGamePlatform = "PC";
    private static readonly Guid FakeGameId = Guid.NewGuid();
    private static readonly Guid FakeDoesNotExistGameId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeGameWishlistsList = new List<GameWishlist>()
        {
            new()
            {
                UserId = FakeUserId,
                GameId = FakeGameId,
                Platform = FakeGamePlatform,
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

        RemoveGameWishlistHandler = new RemoveGameWishlistHandler(InMemDatabase);
    }

    [TestMethod]
    public async Task RemoveGameWishlist_Exists()
    {
        // Arrange
        var command = new RemoveGameWishlistCommand
        {
            UserId = FakeUserId,
            GameId = FakeGameId, 
            Platform = FakeGamePlatform
        };
        
        // Act
        await RemoveGameWishlistHandler!.Handle(command, CancellationToken.None);

        // Assert
        var count = await InMemDatabase!.GameWishlists
            .Where(gw => gw.UserId.Equals(FakeUserId) 
                        && gw.GameId.Equals(FakeGameId)
                        && gw.Platform.Equals(FakeGamePlatform))
            .CountAsync();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task RemoveBookTracking_NotExists()
    {
        // Arrange
        var command = new RemoveGameWishlistCommand 
        { 
            UserId = FakeUserId, 
            GameId = FakeDoesNotExistGameId, 
            Platform = FakeGamePlatform 
        };

        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => 
            RemoveGameWishlistHandler!.Handle(command, CancellationToken.None));
    }
}