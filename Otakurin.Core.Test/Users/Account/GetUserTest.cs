using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Users.Account;
using Otakurin.Domain.User;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Otakurin.Persistence;

namespace Otakurin.Core.Test.Users.Account;

[TestClass]
public class GetUserTest
{
    private static SqliteConnection? Connection { get; set; }
    
    private static DbContextOptions<DatabaseContext>? ContextOptions { get; set; }
    
    private static DatabaseContext? InMemDatabase { get; set; }
    
    private static IMapper? Mapper { get; set; }
    
    private static GetUserHandler? GetUserHandler { get; set; }

    private static readonly Guid FakeUserId = Guid.NewGuid();
    
    [ClassInitialize]
    public static async Task TestClassInit(TestContext context)
    {
        var fakeUserAccount = new UserAccount()
        {
            Id = FakeUserId,
            Email = "bofa@example.com",
            UserName = "bofa",
        };
        
        var fakeUserProfile = new UserProfile()
        {
            UserId = FakeUserId,
            Bio = "BOFADEEEEEEEZ",
            ProfilePictureURL = "sheesh.com",
        };
        
        // Arrange in memory database
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        ContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(Connection)
            .Options;

        InMemDatabase = new DatabaseContext(ContextOptions);
        await InMemDatabase.Database.EnsureCreatedAsync();
        InMemDatabase.Users.Add(fakeUserAccount);
        InMemDatabase.UserProfiles.Add(fakeUserProfile);
        await InMemDatabase.SaveChangesAsync();
        
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddMaps(Assembly.GetAssembly(typeof(Core)));
        });
        Mapper = mappingConfig.CreateMapper();

        GetUserHandler = new GetUserHandler(InMemDatabase, Mapper);
    }

    [TestMethod]
    public async Task GetUser_Found()
    {
        // Arrange
        var query = new GetUserQuery { UserId = FakeUserId };
        
        // Act
        var result = await GetUserHandler!.Handle(query, CancellationToken.None);

        // Assert
        Assert.AreEqual("bofa", result.UserName);
        Assert.AreEqual("bofa@example.com", result.Email);
        Assert.AreEqual("BOFADEEEEEEEZ", result.Bio);
        Assert.AreEqual("sheesh.com", result.ProfilePictureURL);
    }
    
    [TestMethod]
    public async Task GetUser_NotFound()
    {
        // Arrange
        var query = new GetUserQuery { UserId = Guid.NewGuid() };
        
        // Act
        // Assert
        await Assert.ThrowsExceptionAsync<NotFoundException>(() => GetUserHandler!.Handle(query, CancellationToken.None));
    }
}