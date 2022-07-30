using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Exceptions;
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
public class GetShowTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static Mock<IShowService>? MockShowService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static GetShowHandler? GetShowHandler { get; set; }
    
    private static readonly Guid FakeExistShowId = Guid.NewGuid();
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeShow = new Show()
        {
            Id = FakeExistShowId,
            CoverImageURL = "https://chaoschef.example.com",
            Title = "Chaos Chef: Behind the Scenes",
            Summary = "Won Show of the Year",
            ShowType = ShowType.Series
        };
        
        // Arrange in memory database
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

        GetShowHandler = new GetShowHandler(InMemDatabase, MockShowService.Object, Mapper);
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
    public async Task GetShow_Cached()
    {
        // Arrange
        var query = new GetShowQuery { ShowId = FakeExistShowId };

        // Act
        await GetShowHandler!.Handle(query, CancellationToken.None);
    }
 
    [TestMethod]
    public async Task GetShow_NotFound()
    {
        // Arrange
        Guid fakeId = Guid.NewGuid();
        
        var query = new GetShowQuery { ShowId = fakeId };
        
        // Act
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => GetShowHandler!.Handle(query, CancellationToken.None));
    }
}