using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Editor.ConfigurationWindow;
using Sentry.Unity.Editor.WizardApi;
using UnityEngine.TestTools;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests
{
    public sealed class WizardJson
    {
        [Test]
        public void Step1Response()
        {
            var sut = new WizardLoader(new TestLogger());

            var parsed = sut.DeserializeJson<WizardStep1Response>("{\"hash\":\"foo\"}");

            Assert.AreEqual("foo", parsed.hash);
        }

        [Test]
        public void Step2Response()
        {
            var sut = new WizardLoader(new TestLogger());

            var json = "{\"apiKeys\":{\"id\":\"1409625\",\"scopes\":[\"project:releases\"],\"name\":null,\"application\":null,\"expiresAt\":null,\"dateCreated\":\"2024-05-02T11:08:32.565797Z\",\"state\":null,\"token\":\"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\",\"tokenLastCharacters\":\"99bc\"},\"projects\":[{\"slug\":\"sentry-laravel-8\",\"id\":4507183918219264,\"status\":\"active\",\"organization\":{\"id\":447951,\"name\":\"Sentry SDKs\",\"slug\":\"sentry-sdks\",\"region\":\"us\",\"status\":{\"id\":\"active\",\"name\":\"active\"}},\"keys\":[{\"dsn\":{\"public\":\"https://53eedef1b8104c7a3bbd3d0934025695@o447951.ingest.us.sentry.io/4507183918219264\"},\"isActive\":true}]},{\"slug\":\"sentry-devtools\",\"id\":4507182554415104,\"status\":\"active\",\"organization\":{\"id\":1,\"name\":\"Sentry\",\"slug\":\"sentry\",\"region\":\"us\",\"status\":{\"id\":\"active\",\"name\":\"active\"}},\"keys\":[{\"dsn\":{\"public\":\"https://3dc0b17e6467a292dfa9aeaa8e38b6ab@o1.ingest.us.sentry.io/4507182554415104\"},\"isActive\":true}]}]}";
            var parsed = sut.DeserializeJson<WizardStep2Response>(json);

            Assert.NotNull(parsed.apiKeys);
            Assert.AreEqual("api-key-token", parsed.apiKeys!.token);

            Assert.NotNull(parsed.projects);
            Assert.AreEqual(2, parsed.projects.Count);
            var project = parsed.projects[0];
            Assert.AreEqual("project-slug", project.slug);

            Assert.NotNull(project.organization);
            var org = project.organization!;
            Assert.AreEqual("organization-1", org.name);
            Assert.AreEqual("org-slug", org.slug);

            Assert.NotNull(project.keys);
            Assert.AreEqual(1, project.keys!.Count);
            var key = project.keys[0];
            Assert.NotNull(key.dsn);
            Assert.AreEqual("dsn-public", key.dsn!.@public);
        }
    }
}
