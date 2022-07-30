using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Content;
using Otakurin.Core.Exceptions;
using Otakurin.Domain.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Persistence;
using Otakurin.Service.Book;

namespace Otakurin.Core.Test.Books.Content;

[TestClass]
public class GetBookTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static Mock<IBookService>? MockBookService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static GetBookHandler? GetBookHandler { get; set; }
    
    private static readonly Guid FakeExistBookId = Guid.NewGuid();
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBook = new Book()
        {
            Id = FakeExistBookId,
            CoverImageURL = "https://chaoschef.example.com",
            Title = "Chaos Chef: Behind the Scenes",
            Summary = "Won Game of the Year",
            AuthorsString = "Very Indecisive Studios;Overflow"
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.Books.Add(fakeBook);
        await InMemDatabase.SaveChangesAsync();
        
        MockBookService = new Mock<IBookService>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetBookHandler = new GetBookHandler(InMemDatabase, MockBookService.Object, Mapper);
    }

    [ClassCleanup]
    public static async Task TestClassCleanup()
    {
        await Connection!.DisposeAsync();
    }
    
    [TestCleanup]
    public void TestCaseCleanup()
    {
        MockBookService.Reset();
    }
    
    [TestMethod]
    public async Task GetBook_Cached()
    {
        // Arrange
        var query = new GetBookQuery { BookId = FakeExistBookId };

        // Act
        await GetBookHandler!.Handle(query, CancellationToken.None);
    }
 
    [TestMethod]
    public async Task GetBook_NotFound()
    {
        // Arrange
        Guid fakeId = Guid.NewGuid();
        
        var query = new GetBookQuery { BookId = fakeId };
        
        // Act
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => GetBookHandler!.Handle(query, CancellationToken.None));
    }
}