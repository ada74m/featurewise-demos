using System.Security.Claims;
using Featurewise;

namespace Demos.Tests;

// Callers for invoking features directly: the lost property office's traveller and clerk, another
// traveller, and a job.
public static class Callers
{
    public static ClaimsPrincipal Traveller => Person("traveller");

    public static ClaimsPrincipal OtherTraveller => Person("someone-else");

    public static ClaimsPrincipal Clerk => Person("clerk", "clerk");

    public static ClaimsPrincipal Job => SystemCaller.For("test-job");

    private static ClaimsPrincipal Person(string name, params string[] roles) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, name), .. roles.Select(role => new Claim(ClaimTypes.Role, role))],
            authenticationType: "test"));
}
