using System;
using System.Collections.Generic;

namespace Sentry.Unity.Editor.WizardApi;

[Serializable]
internal class WizardStep1Response
{
    public string? hash;
}

[Serializable]
internal class WizardStep2Response
{
    public ApiKeys? apiKeys;
    public List<Project> projects = new List<Project>(0);
}

[Serializable]
internal class ApiKeys
{
    public string? token;
}

[Serializable]
internal class Project
{
    public Organization? organization;
    public string? slug;
    public string? name;
    public string? platform;
    public List<Key>? keys;

    public bool IsUnity => string.Equals(platform, "unity", StringComparison.InvariantCultureIgnoreCase);
}

[Serializable]
internal class Key
{
    public Dsn? dsn;
}

[Serializable]
internal class Dsn
{
    public string? @public;
}

[Serializable]
internal class Organization
{
    public string? name;
    public string? slug;
}
