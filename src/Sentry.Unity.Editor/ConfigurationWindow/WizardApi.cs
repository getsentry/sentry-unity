using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.WizardApi
{
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
        public List<Key>? keys;
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
}
