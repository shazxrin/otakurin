using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Shows.Content;
using Otakurin.Domain.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Persistence;
using Otakurin.Service.Show;

namespace Otakurin.Core.Test.Shows.Content;

[TestClass]
public class FetchShowTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static Mock<IShowService>? MockShowService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static FetchShowHandler? FetchShowHandler { get; set; }
    
    private static readonly Guid FakeExistShowId = Guid.NewGuid();
    
    private static readonly string FakeExistShowRemoteId = "1";
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeShow = new Show()
        {
            Id = FakeExistShowId,
            RemoteId = FakeExistShowRemoteId,
            CoverImageURL = "https://chaoschef.example.com",
            Title = "Chaos Chef: Behind the Scenes",
            Summary = "Won Show of the Year",
            ShowType = ShowType.Series
        };
        
        // Setup in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.Shows.Add(fakeShow);
        await InMemDatabase.SaveChangesAsync();
        
        MockShowService = new Mock<IShowService>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        FetchShowHandler = new FetchShowHandler(InMemDatabase, MockShowService.Object, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }
    
    [TestCleanup]
    public void TestCaseCleanup()
    {
        MockShowService.Reset();
    }
    
    [TestMethod]
    public async Task FetchShow_Exist()
    {
        // Arrange
        var command = new FetchShowCommand { ShowRemoteId = FakeExistShowRemoteId };

        // Act
        var result = await FetchShowHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(result.ShowId, FakeExistShowId);
        MockShowService!.VerifyNoOtherCalls();
    }
 
    [TestMethod]
    public async Task FetchShow_NotExist()
    {
        // Arrange
        var fakeNotExistShowRemoteId = "10000";
        
        var command = new FetchShowCommand { ShowRemoteId = fakeNotExistShowRemoteId };

        MockShowService!
            .Setup(bs => bs.GetShowById(fakeNotExistShowRemoteId))
            .ReturnsAsync(new APIShow(
                fakeNotExistShowRemoteId,
                "",
                "",
                "",
                ShowType.Series
            ));

        // Act
        await FetchShowHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        MockShowService.Verify(bs => bs.GetShowById(fakeNotExistShowRemoteId));
    }
}