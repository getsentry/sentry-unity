using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;

namespace DependencyConflictPackage
{
    /// <summary>
    /// A deliberately tiny stand-in "SDK" whose only purpose is to drag plain,
    /// UNALIASED System.* / Microsoft.* assemblies (at versions that DIFFER from
    /// the ones the real Sentry SDK ships, aliased, in package-dev) into a Unity
    /// project. The API usage below forces real assembly references so the
    /// dependency graph - and therefore the version collision - is genuine.
    /// </summary>
    public static class DependencyConflictPackageClient
    {
        /// <summary>
        /// Exercises every conflicting dependency (System.Text.Json,
        /// System.Collections.Immutable, Microsoft.Bcl.AsyncInterfaces) so they
        /// are genuinely linked into the build, then returns a greeting the
        /// integration test can log to prove the package loaded and ran.
        /// </summary>
        public static async ValueTask<string> SayHiAsync()
        {
            // System.Text.Json
            var payload = JsonSerializer.Serialize(new Greeting { Message = "Dependencies say hi", Level = "info" });

            // System.Collections.Immutable
            var tags = ImmutableArray.Create("sdk:dependency-conflict", "aliased:false");

            // Microsoft.Bcl.AsyncInterfaces (IAsyncDisposable)
            await using var session = new Session();
            await session.SendAsync();

            return $"{JsonSerializer.Deserialize<Greeting>(payload)!.Message} (tags: {tags.Length})";
        }

        private sealed class Greeting
        {
            public string Message { get; set; } = "";
            public string Level { get; set; } = "";
        }

        private sealed class Session : System.IAsyncDisposable
        {
            public Task SendAsync() => Task.CompletedTask;
            public ValueTask DisposeAsync() => default;
        }
    }
}
