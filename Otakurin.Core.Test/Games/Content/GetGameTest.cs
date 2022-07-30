using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Content;
using Otakurin.Core.Exceptions;
using Otakurin.Domain.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Persistence;
using Otakurin.Service.Game;

namespace Otakurin.Core.Test.Games.Content;

[TestClass]
public class GetGameTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static Mock<IGameService>? MockGameService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static GetGameHandler? GetGameHandler { get; set; }
    
    private static readonly Guid FakeExistingGameId = Guid.NewGuid();
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeGame = new Game()
        {
            Id = FakeExistingGameId,
            CoverImageURL = "https://chaoschef.example.com",
            Title = "Chaos Chef: Behind the Scenes",
            Summary = "Won Game of the Year",
            PlatformsString = "PS5",
            CompaniesString = "Very Indecisive Studios;Overflow"
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
        await InMemDatabase.SaveChangesAsync();
        
        MockGameService = new Mock<IGameService>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetGameHandler = new GetGameHandler(InMemDatabase, MockGameService.Object, Mapper);
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
    public async Task GetGame_Default()
    {
        // Arrange
        var query = new GetGameQuery { GameId = FakeExistingGameId };

        // Act
        await GetGameHandler!.Handle(query, CancellationToken.None);
    }
 
    [TestMethod]
    public async Task GetGame_NotFound()
    {
        // Arrange
        Guid fakeId = Guid.NewGuid();
        
        var query = new GetGameQuery { GameId = fakeId };
        
        // Act
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => GetGameHandler!.Handle(query, CancellationToken.None));
    }
}