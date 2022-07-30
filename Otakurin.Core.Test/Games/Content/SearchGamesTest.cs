using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Games.Content;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Service.Game;

namespace Otakurin.Core.Test.Games.Content;

[TestClass]
public class SearchGamesTest
{
    private static Mock<IGameService>? MockGameService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static SearchGamesHandler? SearchGamesHandler { get; set; }

    [ClassInitialize]
    public static void TestClassInit(TestContext context)
    {
        MockGameService = new Mock<IGameService>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        SearchGamesHandler = new SearchGamesHandler(MockGameService.Object, Mapper);
    }

    [TestCleanup]
    public void TestCaseCleanup()
    {
        MockGameService.Reset();
    }

    [TestMethod]
    [DataRow("ch")]
    [DataRow("chaos")]
    [DataRow("chaos chef")]
    public async Task SearchGames_APIHit(string gameTitle)
    {
        // Arrange
        var fakeAPIGames = new List<APIGameBasic>
        {
            new(42069, "http://image.example.com", "Chaos Chef", new List<string> { "PS5" }),
            new(12345, "http://image2.example.com", "Chaos Chef Ultimate", new List<string> { "PS4" })
        };
        
        MockGameService!
            .Setup(service => service.SearchGameByTitle(
                It.Is<string>(s => "chaos chef".Contains(s.ToLower()))))
            .ReturnsAsync(fakeAPIGames);
        
        // Act
        var result = await SearchGamesHandler!.Handle(new SearchGamesQuery { Title = gameTitle }, CancellationToken.None);
        
        // Assert
        MockGameService.Verify(service => service.SearchGameByTitle(gameTitle), Times.Once);
        Assert.AreEqual(2,result.Items.Count);
        Assert.IsNotNull(result.Items.Find(g => g.RemoteId == fakeAPIGames[0].Id));
        Assert.IsNotNull(result.Items.Find(g => g.RemoteId == fakeAPIGames[1].Id));
    }
    
    [TestMethod]
    [DataRow("sma")]
    [DataRow("smash balls")]
    [DataRow("risa_smash")]
    public async Task SearchGames_APINoHit(string gameTitle)
    {
        // Arrange
        MockGameService!
            .Setup(service => service.SearchGameByTitle(It.IsAny<string>()))
            .ReturnsAsync(new List<APIGameBasic>());
        
        // Act
        var result = await SearchGamesHandler!.Handle(new SearchGamesQuery { Title = gameTitle }, CancellationToken.None);
        
        // Assert
        MockGameService.Verify(service => service.SearchGameByTitle(gameTitle), Times.Once);
        Assert.AreEqual(0,result.Items.Count);
    }
}