using System.Security.Claims;
using Featurewise;

namespace Demos.Tests;

// Callers for invoking features directly: the box office's theatregoer and staff, another theatregoer,
// and a job.
public static class Callers
{
    public static ClaimsPrincipal Theatregoer => Person("theatregoer");

    public static ClaimsPrincipal OtherTheatregoer => Person("another-theatregoer");

    public static ClaimsPrincipal Staff => Person("box-office-staff", "staff");

    public static ClaimsPrincipal Job => SystemCaller.For("test-job");

    private static ClaimsPrincipal Person(string name, params string[] roles) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, name), .. roles.Select(role => new Claim(ClaimTypes.Role, role))],
            authenticationType: "test"));
}
