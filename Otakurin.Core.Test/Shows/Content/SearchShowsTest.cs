using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Shows.Content;
using Otakurin.Domain.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Otakurin.Service.Show;

namespace Otakurin.Core.Test.Shows.Content;

[TestClass]
public class SearchShowsTest
{
    private static Mock<IShowService>? MockShowService { get; set; }

    private static IMapper? Mapper { get; set; }
    
    private static SearchShowsHandler? SearchShowsHandler { get; set; }

    [ClassInitialize]
    public static void TestClassInit(TestContext context)
    {
        MockShowService = new Mock<IShowService>();
        
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        SearchShowsHandler = new SearchShowsHandler(MockShowService.Object, Mapper);
    }

    [TestCleanup]
    public void TestCaseCleanup()
    {
        MockShowService.Reset();
    }

    [TestMethod]
    [DataRow("everything")]
    [DataRow("everything everywhere")]
    [DataRow("everything everywhere all")]
    [DataRow("everything everywhere all at")]
    [DataRow("everything everywhere all at once")]
    public async Task SearchShows_APIHit(string showTitle)
    {
        var fakeAPIShows = new List<APIShowBasic>
        {
            new("m_42069", "http://image.example.com", "everything everywhere all at once - movie", ShowType.Movie),
            new("s_420", "http://image.example.com", "everything everywhere all at once - the making of series", ShowType.Series)
        };
        
        MockShowService!
            .Setup(service => service.SearchShowByTitle(
                It.Is<string>(s => "everything everywhere all at once".Contains(s.ToLower()))))
            .ReturnsAsync(fakeAPIShows);
        
        var result = await SearchShowsHandler!.Handle(new SearchShowsQuery { Title = showTitle }, CancellationToken.None);
        
        MockShowService.Verify(service => service.SearchShowByTitle(showTitle), Times.Once);
        Assert.AreEqual(2,result.Items.Count);
        Assert.IsNotNull(result.Items.Find(s => s.RemoteId == fakeAPIShows[0].Id));
        Assert.IsNotNull(result.Items.Find(s => s.RemoteId == fakeAPIShows[1].Id));
    }
    
    [TestMethod]
    [DataRow("sma")]
    [DataRow("smash balls")]
    [DataRow("risa_smash")]
    public async Task SearchShows_APINoHit(string showTitle)
    {
        MockShowService!
            .Setup(service => service.SearchShowByTitle(It.IsAny<string>()))
            .ReturnsAsync(new List<APIShowBasic>());
        
        var result = await SearchShowsHandler!.Handle(new SearchShowsQuery { Title = showTitle }, CancellationToken.None);
        
        MockShowService.Verify(service => service.SearchShowByTitle(showTitle), Times.Once);
        Assert.AreEqual(0,result.Items.Count);
    }
}