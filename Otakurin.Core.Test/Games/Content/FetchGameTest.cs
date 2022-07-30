using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Content;
using Otakurin.Domain.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Persistence;
using Otakurin.Service.Game;

namespace Otakurin.Core.Test.Games.Content;

[TestClass]
public class FetchGameTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static Mock<IGameService>? MockGameService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static FetchGameHandler? FetchGameHandler { get; set; }
    
    private static readonly Guid FakeExistingGameId = Guid.NewGuid();
    
    private static readonly long FakeExistingGameRemoteId = 1;
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeGame = new Game()
        {
            Id = FakeExistingGameId,
            RemoteId = FakeExistingGameRemoteId,
            CoverImageURL = "https://chaoschef.example.com",
            Title = "Chaos Chef: Behind the Scenes",
            Summary = "Won Game of the Year",
            PlatformsString = "PS5",
            CompaniesString = "Very Indecisive Studios;Overflow"
        };
        
        // Setup in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.Games.Add(fakeGame);
        await InMemDatabase.SaveChangesAsync();
        
        MockGameService = new Mock<IGameService>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        FetchGameHandler = new FetchGameHandler(InMemDatabase, MockGameService.Object, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }
    
    [TestCleanup]
    public void TestCaseCleanup()
    {
        MockGameService.Reset();
    }
    
    [TestMethod]
    public async Task FetchGame_Exist()
    {
        // Arrange
        var command = new FetchGameCommand { GameRemoteId = FakeExistingGameRemoteId };

        // Act
        var result = await FetchGameHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(result.GameId, FakeExistingGameId);
        MockGameService!.VerifyNoOtherCalls();
    }
 
    [TestMethod]
    public async Task FetchGame_NotExist()
    {
        // Arrange
        var fakeNotExistGameRemoteId = 10000;
        
        var command = new FetchGameCommand { GameRemoteId = fakeNotExistGameRemoteId };

        MockGameService!
            .Setup(bs => bs.GetGameById(fakeNotExistGameRemoteId))
            .ReturnsAsync(new APIGame(
                fakeNotExistGameRemoteId,
                "",
                "",
                "",
                new List<string>(),
                new List<string>()
            ));

        // Act
        await FetchGameHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        MockGameService.Verify(bs => bs.GetGameById(fakeNotExistGameRemoteId));
    }
}