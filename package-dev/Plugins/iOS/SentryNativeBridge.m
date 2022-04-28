#import <Sentry/Sentry.h>

NS_ASSUME_NONNULL_BEGIN

// macOS only
int SentryNativeBridgeLoadLibrary() { return 0; }
void *SentryNativeBridgeOptionsNew() { return nil; }
void SentryNativeBridgeOptionsSetString(void *options, const char *name, const char *value) { }
void SentryNativeBridgeOptionsSetInt(void *options, const char *name, int32_t value) { }
void SentryNativeBridgeStartWithOptions(void *options) { }

int SentryNativeBridgeCrashedLastRun() { return [SentrySDK crashedLastRun] ? 1 : 0; }

void SentryNativeBridgeClose() { [SentrySDK close]; }

void SentryNativeBridgeAddBreadcrumb(
    const char *timestamp, const char *message, const char *type, const char *category, int level)
{
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope *scope) {
        SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc]
            initWithLevel:level
                 category:(category ? [NSString stringWithUTF8String:category] : nil)];

        if (timestamp != NULL) {
            NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
            [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
            breadcrumb.timestamp =
                [dateFormatter dateFromString:[NSString stringWithUTF8String:timestamp]];
        }

        if (message != NULL) {
            breadcrumb.message = [NSString stringWithUTF8String:message];
        }

        if (type != NULL) {
            breadcrumb.type = [NSString stringWithUTF8String:type];
        }

        [scope addBreadcrumb:breadcrumb];
    }];
}

void SentryNativeBridgeSetExtra(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope *scope) {
        if (value != NULL) {
            [scope setExtraValue:[NSString stringWithUTF8String:value]
                          forKey:[NSString stringWithUTF8String:key]];
        } else {
            [scope removeExtraForKey:[NSString stringWithUTF8String:key]];
        }
    }];
}

void SentryNativeBridgeSetTag(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope *scope) {
        if (value != NULL) {
            [scope setTagValue:[NSString stringWithUTF8String:value]
                        forKey:[NSString stringWithUTF8String:key]];
        } else {
            [scope removeTagForKey:[NSString stringWithUTF8String:key]];
        }
    }];
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    [SentrySDK configureScope:^(
        SentryScope *scope) { [scope removeTagForKey:[NSString stringWithUTF8String:key]]; }];
}

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username)
{
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope *scope) {
        SentryUser *user = [[SentryUser alloc] init];

        if (email != NULL) {
            user.email = [NSString stringWithUTF8String:email];
        }

        if (userId != NULL) {
            user.userId = [NSString stringWithUTF8String:userId];
        }

        if (ipAddress != NULL) {
            user.ipAddress = [NSString stringWithUTF8String:ipAddress];
        }

        if (username != NULL) {
            user.username = [NSString stringWithUTF8String:username];
        }

        [scope setUser:user];
    }];
}

void SentryNativeBridgeUnsetUser()
{
    [SentrySDK configureScope:^(SentryScope *scope) { [scope setUser:nil]; }];
}

NS_ASSUME_NONNULL_END
