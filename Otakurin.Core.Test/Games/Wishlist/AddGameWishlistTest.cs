using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Wishlist;
using Otakurin.Domain.Media;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;
using Otakurin.Service.Game;

namespace Otakurin.Core.Test.Games.Wishlist;

[TestClass]
public class AddGameWishlistTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static AddGameWishlistHandler? AddGameWishlistHandler { get; set; }

    private static readonly Guid FakeExistingGameId = Guid.NewGuid();
    private static readonly Guid FakeExistingUserId = Guid.NewGuid();

    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeUser = new UserAccount()
        {
            Id = FakeExistingUserId
        };

        var fakeGame = new Game()
        {
            Id = FakeExistingGameId
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();

        InMemDatabase.Games.Add(fakeGame);
        InMemDatabase.Users.Add(fakeUser);

        await InMemDatabase.SaveChangesAsync();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        AddGameWishlistHandler = new AddGameWishlistHandler(InMemDatabase, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }
    
    [TestMethod]
    public async Task AddGameWishlist_Default()
    {
        // Arrange
        var fakePlatform = "PC";
        var command = new AddGameWishlistCommand
        {
            UserId = FakeExistingUserId,
            GameId = FakeExistingGameId,
            Platform = fakePlatform
        };
        
        // Act
        await AddGameWishlistHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        var gameWishlist = await InMemDatabase!.GameWishlists
            .Where(gw => gw.GameId == FakeExistingGameId 
                         && gw.UserId.Equals(FakeExistingUserId)
                         && gw.Platform.Equals(fakePlatform))
            .CountAsync();
        Assert.AreEqual(1, gameWishlist);
    }

    [TestMethod]
    public async Task AddGameWishlist_TrackingExists()
    {
        // Arrange
        var fakePlatform = "PSVita";
        var command = new AddGameWishlistCommand
        {
            UserId = FakeExistingUserId,
            GameId = FakeExistingGameId,
            Platform = fakePlatform
        };

        // Act 
        await AddGameWishlistHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        await Assert.ThrowsExceptionAsync<ExistsException>(() => AddGameWishlistHandler.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddGameWishlist_GameNotFound()
    {
        // Arrange
        var fakePlatform = "PC";
        var command = new AddGameWishlistCommand
        {
            UserId = FakeExistingUserId,
            GameId = Guid.NewGuid(),
            Platform = fakePlatform
        }; 

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddGameWishlistHandler!.Handle(command, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task AddGameWishlist_UserNotFound()
    {
        // Arrange
        var fakeAPIGame = new APIGame(
            2,
            "http://image.example.com",
            "Chaos Chef Remastered",
            "Won Game of the Year",
            new List<string> { "PC" },
            new List<string> { "Very Indecisive Studios" }
        );

        var fakePlatform = "PC";
        var command = new AddGameWishlistCommand
        {
            UserId = Guid.NewGuid(),
            GameId = FakeExistingGameId,
            Platform = fakePlatform
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => AddGameWishlistHandler!.Handle(command, CancellationToken.None));
    }
}