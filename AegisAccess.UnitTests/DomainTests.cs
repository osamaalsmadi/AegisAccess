using AegisAccess.Domain.Entities;
using Xunit;
namespace AegisAccess.UnitTests;
public class DomainTests {
    [Fact] public void Resource_HasValidDefaultSensitivity() {
        var r = new Resource { Name = "Test" };
        Assert.Equal("Low", r.SensitivityLevel);
    }
    [Fact] public void User_IsActive_DefaultsToTrue() {
        var u = new User { Username = "test" };
        Assert.True(u.IsActive);
    }
}