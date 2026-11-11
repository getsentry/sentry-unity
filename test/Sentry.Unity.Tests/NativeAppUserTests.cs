using NUnit.Framework;

namespace Sentry.Unity.Tests;

public class NativeAppUserTests
{
    private const string InstallationId = "installation-id";

    private SentryUser? _appUser;

    private NativeAppUser GetSut() =>
        new(new SentryUnityOptions { DefaultUserId = InstallationId }, () => _appUser);

    private static SentryEvent GetEvent(SentryUser user) => new() { User = user };

    [Test]
    public void Process_InstallationIdUser_UsesAppUser()
    {
        _appUser = new SentryUser { Id = "app-user", Email = "app@example.com" };

        var @event = GetSut().Process(GetEvent(new SentryUser { Id = InstallationId }));

        Assert.AreEqual("app-user", @event.User.Id);
        Assert.AreEqual("app@example.com", @event.User.Email);
    }

    [Test]
    public void Process_EmptyUser_UsesAppUser()
    {
        _appUser = new SentryUser { Id = "app-user" };

        var @event = GetSut().Process(GetEvent(new SentryUser()));

        Assert.AreEqual("app-user", @event.User.Id);
    }

    [Test]
    public void Process_NoAppUser_UsesInstallationId()
    {
        _appUser = null;

        var @event = GetSut().Process(GetEvent(new SentryUser()));

        Assert.AreEqual(InstallationId, @event.User.Id);
    }

    [Test]
    public void Process_UserSetByGame_KeepsIt()
    {
        _appUser = new SentryUser { Id = "app-user" };

        var @event = GetSut().Process(GetEvent(new SentryUser { Id = "game-user" }));

        Assert.AreEqual("game-user", @event.User.Id);
    }

    [Test]
    public void Process_UsernameSetByGame_KeepsIt()
    {
        _appUser = new SentryUser { Id = "app-user" };

        var @event = GetSut().Process(GetEvent(new SentryUser { Username = "player1" }));

        Assert.AreEqual("player1", @event.User.Username);
        Assert.IsNull(@event.User.Id);
    }

    [Test]
    public void Process_UsernameAddedByGameToInstallationId_KeepsIt()
    {
        _appUser = new SentryUser { Id = "app-user" };

        var @event = GetSut().Process(GetEvent(new SentryUser { Id = InstallationId, Username = "player1" }));

        Assert.AreEqual(InstallationId, @event.User.Id);
        Assert.AreEqual("player1", @event.User.Username);
    }

    [Test]
    public void Apply_AppUserChanges_FollowsApp()
    {
        var sut = GetSut();
        var scope = new Scope(new SentryOptions());
        _appUser = new SentryUser { Id = "app-user" };
        sut.Apply(scope);

        _appUser = new SentryUser { Id = "other-app-user" };
        sut.Apply(scope);

        Assert.AreEqual("other-app-user", scope.User.Id);
    }

    [Test]
    public void Process_AppUserChangedAfterScopeWasSet_FollowsApp()
    {
        var sut = GetSut();
        var scope = new Scope(new SentryOptions());
        _appUser = new SentryUser { Id = "app-user" };
        sut.Apply(scope);

        _appUser = new SentryUser { Id = "other-app-user" };
        var first = sut.Process(GetEvent(scope.User.Clone()));
        var second = sut.Process(GetEvent(scope.User.Clone()));

        Assert.AreEqual("other-app-user", first.User.Id);
        Assert.AreEqual("other-app-user", second.User.Id);
    }

    [Test]
    public void Apply_AppUserRemoved_FallsBackToInstallationId()
    {
        var sut = GetSut();
        var scope = new Scope(new SentryOptions());
        _appUser = new SentryUser { Id = "app-user" };
        sut.Apply(scope);

        _appUser = null;
        sut.Apply(scope);

        Assert.AreEqual(InstallationId, scope.User.Id);
    }

    [Test]
    public void Apply_UserSetByGameAfterApp_KeepsIt()
    {
        var sut = GetSut();
        var scope = new Scope(new SentryOptions());
        _appUser = new SentryUser { Id = "app-user" };
        sut.Apply(scope);

        scope.User = new SentryUser { Id = "game-user" };
        sut.Apply(scope);

        Assert.AreEqual("game-user", scope.User.Id);
    }

    [Test]
    public void Process_SendDefaultPiiDefaults_UsesAppUserAndKeepsIpAddress()
    {
        var sut = GetSut();
        var scope = new Scope(new SentryOptions());
        scope.User = new SentryUser { Id = InstallationId, Username = "device-user" };
        sut.Initialize(scope);
        _appUser = new SentryUser { Id = "app-user" };

        var @event = sut.Process(GetEvent(
            new SentryUser { Id = InstallationId, Username = "device-user", IpAddress = "{{auto}}" }));

        Assert.AreEqual("app-user", @event.User.Id);
        Assert.AreEqual("{{auto}}", @event.User.IpAddress);
    }

    [Test]
    public void Initialize_DefaultUserWithoutNativeInstallationId_UsesAppUser()
    {
        var sut = new NativeAppUser(new SentryUnityOptions(), () => _appUser);
        var scope = new Scope(new SentryOptions());
        scope.User = new SentryUser { Id = "dotnet-installation-id", Username = "device-user" };
        _appUser = new SentryUser { Id = "app-user" };

        sut.Initialize(scope);

        Assert.AreEqual("app-user", scope.User.Id);
    }

    [Test]
    public void Initialize_NoAppUser_KeepsDefaultUser()
    {
        var sut = new NativeAppUser(new SentryUnityOptions(), () => _appUser);
        var scope = new Scope(new SentryOptions());
        scope.User = new SentryUser { Id = "dotnet-installation-id", Username = "device-user" };
        _appUser = null;

        sut.Initialize(scope);
        var @event = sut.Process(GetEvent(scope.User.Clone()));

        Assert.AreEqual("dotnet-installation-id", @event.User.Id);
        Assert.AreEqual("device-user", @event.User.Username);
    }

    [Test]
    public void FromJson_SerializedCocoaUser_ParsesFields()
    {
        var user = NativeAppUser.FromJson(
            "{\"id\":\"app-user\",\"email\":\"app@example.com\",\"username\":\"app\",\"ip_address\":\"{{auto}}\"}");

        Assert.NotNull(user);
        Assert.AreEqual("app-user", user!.Id);
        Assert.AreEqual("app@example.com", user.Email);
        Assert.AreEqual("app", user.Username);
        Assert.AreEqual("{{auto}}", user.IpAddress);
    }

    [Test]
    public void FromJson_Null_ReturnsNull() => Assert.IsNull(NativeAppUser.FromJson(null));
}
