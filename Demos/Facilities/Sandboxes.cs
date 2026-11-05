using System.Security.Claims;

namespace Demos;

// Each visitor to the demos works in a sandbox of their own, shared by every persona they sign in as.
// The sign-in writes it into the caller's token as a claim. Only the facilities read it, to keep each
// sandbox's data apart, and no feature does. A job or an event carries its sandbox on its caller too.
public static class Sandboxes
{
    public const string ClaimType = "sandbox";

    // The sandbox a caller without the claim works in, as on a laptop, where the dev users have none.
    public const string Default = "local";

    public static string Of(ClaimsPrincipal caller) => caller.FindFirst(ClaimType)?.Value ?? Default;

    // The caller, working in the sandbox.
    public static ClaimsPrincipal In(this ClaimsPrincipal caller, string sandbox)
    {
        caller.AddIdentity(new ClaimsIdentity([new Claim(ClaimType, sandbox)]));
        return caller;
    }
}
