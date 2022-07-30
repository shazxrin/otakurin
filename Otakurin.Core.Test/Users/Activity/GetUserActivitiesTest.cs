using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Users.Activity;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Users.Activity;

[TestClass]
public class GetUserActivitiesTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static IMapper? Mapper { get; set; }
    
    private static GetUserActivitiesHandler? GetUserActivitiesHandler { get; set; }
    
    private static readonly Guid FakeUser1Id = Guid.NewGuid();
    private static readonly Guid FakeUser2Id = Guid.NewGuid();
    private static readonly Guid FakeUser3Id = Guid.NewGuid();


    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeUserList = new List<UserAccount>
        {
            new ()
            {
                Id = FakeUser1Id,
                UserName = "User1"
            },
            new ()
            {
                Id = FakeUser2Id,
                UserName = "User2"
            },
            new ()
            {
                Id = FakeUser3Id,
                UserName = "User3"
            }
        };
        
        var fakeActivitiesList = new List<UserActivity>
        {
            new () 
            {
                UserId = FakeUser1Id
            },
            new () 
            {
                UserId = FakeUser2Id
            },
            new () 
            {
                UserId = FakeUser2Id
            },
            new () 
            {
                UserId = FakeUser3Id
            },
            new () 
            {
                UserId = FakeUser3Id
            },            
            new () 
            {
                UserId = FakeUser3Id
            }
        };
        
        var fakeUserProfileList = new List<UserProfile>
        {
            new ()
            {
                UserId = FakeUser1Id,
            },
            new ()
            {
                UserId = FakeUser2Id,
            },
            new ()
            {
                UserId = FakeUser3Id,
            }
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        await InMemDatabase.Users.AddRangeAsync(fakeUserList);
        await InMemDatabase.UserProfiles.AddRangeAsync(fakeUserProfileList);
        await InMemDatabase.Activities.AddRangeAsync(fakeActivitiesList);
        await InMemDatabase.SaveChangesAsync();
        
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetUserActivitiesHandler = new GetUserActivitiesHandler(InMemDatabase);
    }

    [TestMethod]
    public async Task GetUserActivities_Default()
    {
        // Arrange
        var query1 = new GetUserActivitiesQuery { UserId = FakeUser1Id };
        var query2 = new GetUserActivitiesQuery { UserId = FakeUser2Id };
        var query3 = new GetUserActivitiesQuery { UserId = FakeUser3Id };
        
        // Act
        var result1 = await GetUserActivitiesHandler!.Handle(query1, CancellationToken.None);
        var result2 = await GetUserActivitiesHandler!.Handle(query2, CancellationToken.None);
        var result3 = await GetUserActivitiesHandler!.Handle(query3, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, result1.Items.Count);
        Assert.AreEqual(2, result2.Items.Count);
        Assert.AreEqual(3, result3.Items.Count);
    }
}