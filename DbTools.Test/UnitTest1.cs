using DbTools.Test.TestModels;
using Xunit.Abstractions;

namespace DbTools.Test;

public class UnitTest1 {
    private readonly ITestOutputHelper _output;

    public UnitTest1(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public async void InsertTest() {
        await using var db = new TestDb();
        var id = await db.InsertAsync<Gender>(new Gender {
            Name = "Test name"
        });
        Assert.NotNull(id);
        Gender? testObj = await db.GetByIdAsync<Gender>(Convert.ToInt32(id));
        Assert.NotNull(testObj);
        await db.RemoveAsync(testObj);
    }

    [Fact]
    public async void GetTest() {
        await using var db = new TestDb();
        var list = await db.GetAsync<Gender>().ToListAsync();
        Assert.NotNull(list);
    }

    [Fact]
    public async void GetForeignKeysMultipleTest() {
        await using var db = new TestDb();
        var list = await db.GetAsync<User>().ToListAsync();
        Assert.All(list, user => {
            Assert.NotNull(user);
            Assert.NotNull(user.Gender);
        });
    }

    [Fact]
    public async void GetForeignKeysSingularTest() {
        await using var db = new TestDb();
        var obj = await db.GetByIdAsync<User>(1);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Gender);
    }
    
    
    [Fact]
    public async void UpdateTest() {
        await using var db = new TestDb();
        var userToUpdate = new User() {
            FullName = Guid.NewGuid().ToString(),
            GenderId = 1
        };
        await db.UpdateAsync(1, userToUpdate);
        var updatedUser = await db.GetByIdAsync<User>(1);
        Assert.NotNull(updatedUser);
        Assert.Equal(userToUpdate.FullName, updatedUser.FullName);
    }

    [Fact]
    public async void RemoveTest() {
        await using var db = new TestDb();
        var list = await db.GetAsync<User>().ToListAsync();
        var userToRemove = list.Last();
        Assert.NotNull(userToRemove);
        await db.RemoveAsync(userToRemove);
        var removedUser = await db.GetByIdAsync<User>(userToRemove.Id);
        Assert.Null(removedUser);
    }
}