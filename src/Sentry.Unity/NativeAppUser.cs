using System;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity;

/// <summary>
/// The user of a native SDK that the app embedding the game owns, e.g. when using Unity as a Library. The game's events
/// carry it, read on every event, and so do its logs, read at start and whenever the game resumes. A user the game
/// sets itself takes precedence.
/// </summary>
internal sealed class NativeAppUser : ISentryEventProcessor, ISdkIntegration
{
    private readonly SentryUnityOptions _options;
    private readonly Func<SentryUser?> _getAppUser;
    // The app's user as last put on the scope, to tell it apart from a user the game set.
    private volatile SentryUser? _applied;

    internal NativeAppUser(SentryUnityOptions options, Func<SentryUser?> getAppUser)
    {
        _options = options;
        _getAppUser = getAppUser;
    }

    internal static void Use(SentryUnityOptions options, Func<SentryUser?> getAppUser)
    {
        var appUser = new NativeAppUser(options, getAppUser);
        options.AddEventProcessor(appUser);
        options.AddIntegration(appUser);
    }

    public void Register(IHub hub, SentryOptions options)
    {
        hub.ConfigureScope(Apply);
        SentryMonoBehaviour.Instance.ApplicationResuming += () =>
        {
            if (hub.IsEnabled)
            {
                hub.ConfigureScope(Apply);
            }
        };
    }

    public SentryEvent Process(SentryEvent @event)
    {
        if (IsOwnedByApp(@event.User))
        {
            @event.User = GetUser().Clone();
        }

        return @event;
    }

    internal void Apply(Scope scope)
    {
        if (IsOwnedByApp(scope.User))
        {
            var user = GetUser();
            _applied = user;
            scope.User = user.Clone();
        }
    }

    // The app's user, or the installation ID while the app has none.
    private SentryUser GetUser()
    {
        SentryUser? appUser = null;
        try
        {
            appUser = _getAppUser();
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogWarning("Failed to get the app's user.", e);
        }

        return appUser is { } && HasData(appUser) ? appUser : new SentryUser { Id = _options.DefaultUserId };
    }

    // Not set by the game: empty, the installation ID, or the app's user set here before.
    private bool IsOwnedByApp(SentryUser user) =>
        !HasData(user)
        || (user.Id == _options.DefaultUserId && user.Username is null && user.Email is null && user.IpAddress is null)
        || (_applied is { } applied && user.Id == applied.Id && user.Username == applied.Username
            && user.Email == applied.Email && user.IpAddress == applied.IpAddress);

    private static bool HasData(SentryUser user) =>
        user.Id is not null || user.Username is not null || user.Email is not null || user.IpAddress is not null;

    internal static SentryUser? FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json!);
        return SentryUser.FromJson(document.RootElement);
    }
}
