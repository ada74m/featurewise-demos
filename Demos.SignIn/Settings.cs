namespace Demos.SignIn;

// The one client the sign-in serves, the UI shell: its id, where it may be sent back to after signing
// in, and where after logging out.
public record Settings(string ClientId, string RedirectUri, string LogoutUri)
{
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(8);

    // A one-time code is traded for tokens straight away.
    public TimeSpan CodeLifetime { get; init; } = TimeSpan.FromMinutes(2);

    // A visitor keeps their sandbox for a week, as long as the demos keep its data.
    public TimeSpan SandboxLifetime { get; init; } = TimeSpan.FromDays(7);

    // The shell's origin, which may read the token endpoint's answers.
    public string ShellOrigin => new Uri(RedirectUri).GetLeftPart(UriPartial.Authority);
}
