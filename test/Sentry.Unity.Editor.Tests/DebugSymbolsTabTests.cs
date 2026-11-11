using System;
using NUnit.Framework;
using Sentry.Unity.Editor.ConfigurationWindow;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests;

public class DebugSymbolsTabTests
{
    private string? _originalSentryOrg;
    private SentryCliOptions _cliOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _originalSentryOrg = Environment.GetEnvironmentVariable("SENTRY_ORG");
        Environment.SetEnvironmentVariable("SENTRY_ORG", null);

        _cliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        _cliOptions.Auth = "sntryu_personal_token";
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("SENTRY_ORG", _originalSentryOrg);
        UnityEngine.Object.DestroyImmediate(_cliOptions);
    }

    [Test]
    public void IsOrganizationMissing_PersonalTokenWithoutOrganization_ReturnsTrue()
    {
        Assert.IsTrue(DebugSymbolsTab.IsOrganizationMissing(_cliOptions));
    }

    [Test]
    public void IsOrganizationMissing_PersonalTokenWithOrganization_ReturnsFalse()
    {
        _cliOptions.Organization = "test-org";

        Assert.IsFalse(DebugSymbolsTab.IsOrganizationMissing(_cliOptions));
    }

    [Test]
    public void IsOrganizationMissing_OrganizationTokenWithoutOrganization_ReturnsFalse()
    {
        _cliOptions.Auth = "sntrys_organization_token";

        Assert.IsFalse(DebugSymbolsTab.IsOrganizationMissing(_cliOptions));
    }

    [Test]
    public void IsOrganizationMissing_SentryOrgEnvironmentVariableWithoutOrganization_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("SENTRY_ORG", "test-org");

        Assert.IsFalse(DebugSymbolsTab.IsOrganizationMissing(_cliOptions));
    }
}
