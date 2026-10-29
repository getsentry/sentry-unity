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
    // Set on events by the SDK with `SendDefaultPii`.
    private const string AutoIpAddress = "{{auto}}";

    private readonly SentryUnityOptions _options;
    private readonly Func<SentryUser?> _getAppUser;
    // The SDK's default user on the scope, and the app's user as last put there, to tell them apart from a user the
    // game set.
    private volatile SentryUser? _default;
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
        hub.ConfigureScope(Initialize);
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
            var user = GetUser().Clone();
            user.IpAddress ??= @event.User.IpAddress;
            @event.User = user;
        }

        return @event;
    }

    internal void Initialize(Scope scope)
    {
        _default = scope.User.Clone();
        Apply(scope);
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

    // The app's user, or the SDK's default user while the app has none.
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

        return appUser is { } && HasData(appUser)
            ? appUser
            : _default ?? new SentryUser { Id = _options.DefaultUserId };
    }

    // Not set by the game: each field is unset, the SDK's default, or the app's user's.
    private bool IsOwnedByApp(SentryUser user) =>
        IsOwnedByApp(user.Id, _options.DefaultUserId, _default?.Id, _applied?.Id)
        && IsOwnedByApp(user.Username, null, _default?.Username, _applied?.Username)
        && IsOwnedByApp(user.Email, null, _default?.Email, _applied?.Email)
        && IsOwnedByApp(user.IpAddress, AutoIpAddress, _default?.IpAddress, _applied?.IpAddress);

    private static bool IsOwnedByApp(string? value, string? sdkValue, string? defaultValue, string? appliedValue) =>
        value is null || value == sdkValue || value == defaultValue || value == appliedValue;

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
