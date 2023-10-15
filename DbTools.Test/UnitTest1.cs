using DbTools.Test.TestModels;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DbTools.Test;

public class UnitTest1 {
    
    private readonly ITestOutputHelper output;
    
    public UnitTest1(ITestOutputHelper output)
    {
        this.output = output;
    }
    
    [Fact]
    public async void Test1() {
        await using TestDb db = new TestDb();
        var id = await db.InsertAsync<Gender>(new Gender {
            Name = "Test name"
        });
        output.WriteLine(id!.GetType().ToString());
        Gender testObj = await db.GetByIdAsync<Gender>(Convert.ToInt32(id));

        if (testObj is not null) {
            await db.RemoveAsync(testObj);
        }
    }
}
