using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Books.Content;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Service.Book;

namespace Otakurin.Core.Test.Books.Content;

[TestClass]
public class SearchBooksTest
{
    private static Mock<IBookService>? MockBookService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static SearchBooksHandler? SearchBooksHandler { get; set; }

    [ClassInitialize]
    public static void TestClassInit(TestContext context)
    {
        MockBookService = new Mock<IBookService>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        SearchBooksHandler = new SearchBooksHandler(MockBookService.Object, Mapper);
    }

    [TestCleanup]
    public void TestCaseCleanup()
    {
        MockBookService.Reset();
    }

    [TestMethod]
    [DataRow("ch")]
    [DataRow("chaos")]
    [DataRow("chaos chef")]
    public async Task SearchBooks_APIHit(string gameTitle)
    {
        // Arrange
        var fakeAPIBooks = new List<APIBookBasic>
        {
            new("42069", "http://image.example.com", "Chaos Chef: Manual", new List<string> { "Sterling Kwan" }),
            new("12345", "http://image2.example.com", "Chaos Chef Ultimate: Ultimate Manual", new List<string> { "Bryan Seah" })
        };
        
        MockBookService!
            .Setup(service => service.SearchBookByTitle(
                It.Is<string>(s => "chaos chef".Contains(s.ToLower()))))
            .ReturnsAsync(fakeAPIBooks);
        
        // Act
        var result = await SearchBooksHandler!.Handle(new SearchBooksQuery { Title = gameTitle }, CancellationToken.None);
        
        // Assert
        MockBookService.Verify(service => service.SearchBookByTitle(gameTitle), Times.Once);
        Assert.AreEqual(2,result.Items.Count);
    }
    
    [TestMethod]
    [DataRow("sma")]
    [DataRow("smash balls")]
    [DataRow("risa_smash")]
    public async Task SearchBooks_APINoHit(string gameTitle)
    {
        // Arrange
        MockBookService!
            .Setup(service => service.SearchBookByTitle(It.IsAny<string>()))
            .ReturnsAsync(new List<APIBookBasic>());
        
        // Act
        var result = await SearchBooksHandler!.Handle(new SearchBooksQuery { Title = gameTitle }, CancellationToken.None);
        
        // Assert
        MockBookService.Verify(service => service.SearchBookByTitle(gameTitle), Times.Once);
        Assert.AreEqual(0,result.Items.Count);
    }
}