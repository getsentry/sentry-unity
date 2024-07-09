using NUnit.Framework;
using Sentry.Unity.Editor.ConfigurationWindow;
using Sentry.Unity.Editor.WizardApi;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests;

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

        var json = "{\"apiKeys\":{\"id\":\"key-1\",\"scopes\":[\"org:read\",\"project:read\",\"project:releases\",\"project:write\"],\"application\":null,\"expiresAt\":null,\"dateCreated\":\"2022-03-02T10:37:56.385524Z\",\"state\":null,\"token\":\"api-key-token\",\"refreshToken\":null},\"projects\":[{\"id\":\"project-1\",\"slug\":\"project-slug\",\"name\":\"personal\",\"isPublic\":false,\"isBookmarked\":false,\"color\":\"#3fb7bf\",\"dateCreated\":\"2022-01-15T20:05:53.883628Z\",\"firstEvent\":\"2022-01-15T20:15:10.171648Z\",\"firstTransactionEvent\":false,\"hasSessions\":true,\"features\":[\"alert-filters\",\"issue-alerts-targeting\",\"minidump\",\"performance-suspect-spans-ingestion\",\"race-free-group-creation\",\"similarity-indexing\",\"similarity-view\",\"releases\"],\"status\":\"active\",\"platform\":\"flutter\",\"isInternal\":false,\"isMember\":false,\"hasAccess\":true,\"avatar\":{\"avatarType\":\"letter_avatar\",\"avatarUuid\":null},\"organization\":{\"id\":\"org-1\",\"slug\":\"org-slug\",\"status\":{\"id\":\"active\",\"name\":\"active\"},\"name\":\"organization-1\",\"dateCreated\":\"2022-01-15T20:03:49.620687Z\",\"isEarlyAdopter\":false,\"require2FA\":false,\"requireEmailVerification\":false,\"avatar\":{\"avatarType\":\"letter_avatar\",\"avatarUuid\":null},\"features\":[\"onboarding\",\"ondemand-budgets\",\"slack-overage-notifications\",\"dashboards-template\",\"discover-frontend-use-events-endpoint\",\"integrations-stacktrace-link\",\"crash-rate-alerts\",\"org-subdomains\",\"performance-dry-run-mep\",\"mobile-app\",\"custom-event-title\",\"advanced-search\",\"widget-library\",\"auto-start-free-trial\",\"release-health-return-metrics\",\"minute-resolution-sessions\",\"capture-lead\",\"invite-members-rate-limits\",\"alert-crash-free-metrics\",\"alert-wizard-v3\",\"images-loaded-v2\",\"duplicate-alert-rule\",\"performance-autogroup-sibling-spans\",\"performance-ops-breakdown\",\"new-widget-builder-experience-design\",\"performance-suspect-spans-view\",\"unified-span-view\",\"performance-frontend-use-events-endpoint\",\"widget-viewer-modal\",\"event-attachments\",\"symbol-sources\",\"performance-span-histogram-view\",\"intl-sales-tax\",\"metrics-extraction\",\"performance-view\",\"new-weekly-report\",\"performance-span-tree-autoscroll\",\"metric-alert-snql\",\"shared-issues\",\"dashboard-grid-layout\",\"open-membership\"]},\"keys\":[{\"id\":\"key-1\",\"name\":\"Default\",\"label\":\"Default\",\"public\":\"public-key\",\"secret\":\"secret-key\",\"projectId\":12345,\"isActive\":true,\"rateLimit\":null,\"dsn\":{\"secret\":\"dsn-secret\",\"public\":\"dsn-public\",\"csp\":\"\",\"security\":\"\",\"minidump\":\"\",\"unreal\":\"\",\"cdn\":\"\"},\"browserSdkVersion\":\"6.x\",\"browserSdk\":{\"choices\":[[\"latest\",\"latest\"],[\"7.x\",\"7.x\"],[\"6.x\",\"6.x\"],[\"5.x\",\"5.x\"],[\"4.x\",\"4.x\"]]},\"dateCreated\":\"2022-01-15T20:05:53.895882Z\"}]},{\"id\":\"project-2\",\"slug\":\"trending-movies\",\"name\":\"trending-movies\",\"isPublic\":false,\"isBookmarked\":false,\"color\":\"#bfb93f\",\"dateCreated\":\"2022-06-16T16:34:36.833418Z\",\"firstEvent\":null,\"firstTransactionEvent\":false,\"hasSessions\":false,\"features\":[\"alert-filters\",\"custom-inbound-filters\",\"data-forwarding\",\"discard-groups\",\"issue-alerts-targeting\",\"minidump\",\"performance-suspect-spans-ingestion\",\"race-free-group-creation\",\"rate-limits\",\"servicehooks\",\"similarity-indexing\",\"similarity-indexing-v2\",\"similarity-view\",\"similarity-view-v2\"],\"status\":\"active\",\"platform\":\"apple-ios\",\"isInternal\":false,\"isMember\":false,\"hasAccess\":true,\"avatar\":{\"avatarType\":\"letter_avatar\",\"avatarUuid\":null},\"organization\":{\"id\":\"organization-2\",\"slug\":\"sentry-sdks\",\"status\":{\"id\":\"active\",\"name\":\"active\"},\"name\":\"Sentry SDKs\",\"dateCreated\":\"2020-09-14T17:28:14.933511Z\",\"isEarlyAdopter\":true,\"require2FA\":false,\"requireEmailVerification\":false,\"avatar\":{\"avatarType\":\"upload\",\"avatarUuid\":\"\"},\"features\":[\"mobile-screenshots\",\"integrations-ticket-rules\",\"ondemand-budgets\",\"dashboards-template\",\"metric-alert-chartcuterie\",\"reprocessing-v2\",\"filters-and-sampling\",\"grouping-stacktrace-ui\",\"discover-frontend-use-events-endpoint\",\"grouping-title-ui\",\"app-store-connect-multiple\",\"crash-rate-alerts\",\"org-subdomains\",\"performance-dry-run-mep\",\"mobile-app\",\"grouping-tree-ui\",\"custom-event-title\",\"widget-library\",\"advanced-search\",\"auto-start-free-trial\",\"global-views\",\"integrations-chat-unfurl\",\"release-health-return-metrics\",\"integrations-stacktrace-link\",\"dashboards-basic\",\"minute-resolution-sessions\",\"discover-basic\",\"capture-lead\",\"alert-crash-free-metrics\",\"data-forwarding\",\"custom-symbol-sources\",\"sso-saml2\",\"integrations-alert-rule\",\"alert-wizard-v3\",\"invite-members\",\"team-insights\",\"integrations-issue-sync\",\"images-loaded-v2\",\"duplicate-alert-rule\",\"performance-autogroup-sibling-spans\",\"monitors\",\"new-widget-builder-experience-design\",\"dashboards-edit\",\"integrations-issue-basic\",\"performance-ops-breakdown\",\"performance-suspect-spans-view\",\"unified-span-view\",\"related-events\",\"integrations-event-hooks\",\"performance-frontend-use-events-endpoint\",\"sso-basic\",\"widget-viewer-modal\",\"event-attachments\",\"symbol-sources\",\"performance-span-histogram-view\",\"new-widget-builder-experience\",\"profiling\",\"dashboards-releases\",\"metrics-extraction\",\"integrations-incident-management\",\"new-weekly-report\",\"performance-view\",\"performance-span-tree-autoscroll\",\"open-membership\",\"metric-alert-snql\",\"shared-issues\",\"integrations-codeowners\",\"change-alerts\",\"dashboard-grid-layout\",\"baa\",\"discover-query\",\"alert-filters\",\"incidents\",\"relay\"]},\"keys\":[{\"id\":\"key-2\",\"name\":\"Default\",\"label\":\"Default\",\"public\":\"public\",\"secret\":\"secret\",\"projectId\":6789,\"isActive\":true,\"rateLimit\":null,\"dsn\":{\"secret\":\"dsn-secret\",\"public\":\"dsn-public\",\"csp\":\"\",\"security\":\"\",\"minidump\":\"\",\"unreal\":\"\",\"cdn\":\"\"},\"browserSdkVersion\":\"7.x\",\"browserSdk\":{\"choices\":[[\"latest\",\"latest\"],[\"7.x\",\"7.x\"],[\"6.x\",\"6.x\"],[\"5.x\",\"5.x\"],[\"4.x\",\"4.x\"]]},\"dateCreated\":\"2022-06-16T16:34:36.845197Z\"}]}]}";
        var parsed = sut.DeserializeJson<WizardStep2Response>(json);

        Assert.NotNull(parsed.apiKeys);
        Assert.AreEqual("api-key-token", parsed.apiKeys!.token);

        Assert.NotNull(parsed.projects);
        Assert.AreEqual(2, parsed.projects.Count);
        var project = parsed.projects[0];
        Assert.AreEqual("project-slug", project.slug);
        Assert.AreEqual("personal", project.name);
        Assert.AreEqual("flutter", project.platform);

        Assert.IsFalse(project.IsUnity);
        project.platform = "unity";
        Assert.IsTrue(project.IsUnity);

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