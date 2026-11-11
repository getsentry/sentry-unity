using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class SentryCliOptionsTests
{
    private string? _originalSentryOrg;
    private SentryCliOptions _sut = null!;
    private TestLogger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _originalSentryOrg = Environment.GetEnvironmentVariable(SentryCliOptions.OrganizationEnvironmentVariable);
        Environment.SetEnvironmentVariable(SentryCliOptions.OrganizationEnvironmentVariable, null);

        _sut = ScriptableObject.CreateInstance<SentryCliOptions>();
        _sut.UploadSymbols = true;
        _sut.Auth = "sntryu_personal_token";
        _sut.Project = "test-project";
        _logger = new TestLogger();
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(SentryCliOptions.OrganizationEnvironmentVariable, _originalSentryOrg);
        UnityEngine.Object.DestroyImmediate(_sut);
    }

    [Test]
    public void IsValid_PersonalTokenWithoutOrganization_ReturnsFalse()
    {
        Assert.IsFalse(_sut.IsValid(_logger, false));
        Assert.IsTrue(_logger.Logs.Any(log => log.message.Contains("Org Slug missing")));
    }

    [Test]
    public void IsValid_PersonalTokenWithOrganization_ReturnsTrue()
    {
        _sut.Organization = "test-org";

        Assert.IsTrue(_sut.IsValid(_logger, false));
    }

    [Test]
    public void IsValid_OrganizationTokenWithoutOrganization_ReturnsTrue()
    {
        _sut.Auth = "sntrys_organization_token";

        Assert.IsTrue(_sut.IsValid(_logger, false));
    }

    [Test]
    public void IsValid_SentryOrgEnvironmentVariableWithoutOrganization_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable(SentryCliOptions.OrganizationEnvironmentVariable, "test-org");

        Assert.IsTrue(_sut.IsValid(_logger, false));
    }
}
