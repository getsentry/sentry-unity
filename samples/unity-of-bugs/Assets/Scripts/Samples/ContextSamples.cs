using System;
using System.Text;
using UnityEngine;

namespace Sentry.Unity.Samples
{
    /// <summary>The context samples: the user, tags, breadcrumbs, attachments and an event BeforeSend drops.</summary>
    public static class ContextSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Context",
            new SampleEntry("context/set-user", "Set user",
                "Sets ant@farm.bug as the user on every event that follows.",
                SetUser),
            new SampleEntry("context/message-with-context", "Message with context",
                "Captures a message with a custom PlayerCharacter context.",
                CaptureMessageWithContext),
            new SampleEntry("context/set-tag", "Set tag",
                "Sets the tag sample.tag on every event that follows.",
                SetTag),
            new SampleEntry("context/add-breadcrumb", "Add breadcrumb",
                "Adds a breadcrumb that the next event shows in its trail.",
                AddBreadcrumb),
            new SampleEntry("context/attachment", "Message with an attachment",
                "Adds a text file to the scope and captures a message that carries it.",
                CaptureMessageWithAttachment),
            new SampleEntry("context/dropped-by-before-send", "Dropped by BeforeSend",
                "Captures a message tagged SomeTag, which BeforeSend drops: nothing should arrive.",
                CaptureDroppedMessage));

        private static void SetUser()
        {
            // Setting the user on the scope makes sure the user is set on the context of all future events
            SentrySdk.ConfigureScope(s =>
            {
                s.User = new SentryUser
                {
                    Email = "ant@farm.bug",
                    Username = "ant",
                    Id = "ant-id"
                };
            });
            Debug.Log("User set: ant");
            SampleRunner.Report("User set: ant");
        }

        private class PlayerCharacter
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string AttackType { get; set; }
        }

        private static void CaptureMessageWithContext()
        {
            // The context is freely customizable and allows you to add data specific to your game.
            // The SDK's capture methods take an optional scope that only applies to that one event.
            // An event captured with a scope callback isn't the hub's LastEventId: its ID is the return value.
            var eventId = SentrySdk.CaptureMessage("Capturing with player character context.", scope =>
            {
                scope.Contexts["character"] = new PlayerCharacter
                {
                    Name = "Mighty Fighter",
                    Age = 19,
                    AttackType = "melee"
                };
            });
            SampleRunner.Report($"Last event: {eventId}");
        }

        private static void SetTag()
        {
            SentrySdk.SetTag("sample.tag", "set-from-the-menu");
            SampleRunner.Report("Tag set: sample.tag");
        }

        private static void AddBreadcrumb()
        {
            SentrySdk.AddBreadcrumb("The player picked up a 🐞", "sample", level: BreadcrumbLevel.Info);
            SampleRunner.Report("Breadcrumb added");
        }

        private static void CaptureMessageWithAttachment()
        {
            var text = $"Captured at {DateTime.UtcNow:O} by the Unity of Bugs sample.";
            var eventId = SentrySdk.CaptureMessage("Message with an attachment", scope =>
                scope.AddAttachment(Encoding.UTF8.GetBytes(text), "sample-attachment.txt", AttachmentType.Default, "text/plain"));
            SampleRunner.Report($"Last event: {eventId}");
        }

        private static void CaptureDroppedMessage()
        {
            // SentryOptionConfiguration's BeforeSend drops events with the tag SomeTag
            SentrySdk.CaptureMessage("This message is dropped by BeforeSend", scope => scope.SetTag("SomeTag", "dropped"));
            SampleRunner.Report("Dropped by BeforeSend: nothing should arrive");
        }
    }
}
