using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Content;
using Otakurin.Domain.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Persistence;
using Otakurin.Service.Book;

namespace Otakurin.Core.Test.Books.Content;

[TestClass]
public class FetchBookTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static Mock<IBookService>? MockBookService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static FetchBookHandler? FetchBookHandler { get; set; }
    
    private static readonly Guid FakeExistBookId = Guid.NewGuid();
    
    private static readonly string FakeExistBookRemoteId = "fakeExistBookRemoteId";
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeBook = new Book()
        {
            Id = FakeExistBookId,
            RemoteId = FakeExistBookRemoteId,
            CoverImageURL = "https://chaoschef.example.com",
            Title = "Chaos Chef: Behind the Scenes",
            Summary = "Won Game of the Year",
            AuthorsString = "Very Indecisive Studios;Overflow"
        };
        
        // Setup in memory database
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

        FetchBookHandler = new FetchBookHandler(InMemDatabase, MockBookService.Object, Mapper);
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
    public async Task FetchBook_Exist()
    {
        // Arrange
        var command = new FetchBookCommand { BookRemoteId = FakeExistBookRemoteId };

        // Act
        var result = await FetchBookHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(result.BookId, FakeExistBookId);
        MockBookService!.VerifyNoOtherCalls();
    }
 
    [TestMethod]
    public async Task FetchBook_NotExist()
    {
        // Arrange
        var fakeNotExistBookRemoteId = "FakeNotExistBookRemoteId";
        
        var command = new FetchBookCommand { BookRemoteId = "FakeNotExistBookRemoteId" };

        MockBookService!
            .Setup(bs => bs.GetBookById(fakeNotExistBookRemoteId))
            .ReturnsAsync(new APIBook(
                fakeNotExistBookRemoteId,
                "",
                "",
                "",
                new List<string>()
            ));

        // Act
        await FetchBookHandler!.Handle(command, CancellationToken.None);
        
        // Assert
        MockBookService.Verify(bs => bs.GetBookById(fakeNotExistBookRemoteId));
    }
}