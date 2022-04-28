#import <Sentry/Sentry.h>

NS_ASSUME_NONNULL_BEGIN

int SentryNativeBridgeLoadLibrary()
{
    // macOS only
}

void *SentryNativeBridgeOptionsNew()
{
    // macOS only
}

void SentryNativeBridgeOptionsSetString(void *options, const char *name, const char *value)
{
    // macOS only
}

void SentryNativeBridgeOptionsSetInt(void *options, const char *name, int32_t value)
{
    // macOS only
}

void SentryNativeBridgeStartWithOptions(void *options)
{
    // macOS only
}

int SentryNativeBridgeCrashedLastRun()
{
    @try {
        return [SentrySDK crashedLastRun] ? 1 : 0;
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to get crashedLastRun: %@", exception.reason);
    }
    return -1;
}

void SentryNativeBridgeClose()
{
    @try {
        [SentrySDK close];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to close: %@", exception.reason);
    }
}

void SentryNativeBridgeAddBreadcrumb(
    const char *timestamp, const char *message, const char *type, const char *category, int level)
{
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    @try {
        [SentrySDK configureScope:^(SentryScope *scope) {
            SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc]
                initWithLevel:level
                     category:(category ? [NSString stringWithUTF8String:category] : nil)];

            if (timestamp != NULL) {
                NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
                [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
                breadcrumb.timestamp =
                    [dateFormatter dateFromString:[NSString stringWithUTF8String:timestamp]];
                [dateFormatter release];
            }

            if (message != NULL) {
                breadcrumb.message = [NSString stringWithUTF8String:message];
            }

            if (type != NULL) {
                breadcrumb.type = [NSString stringWithUTF8String:type];
            }

            [scope addBreadcrumb:breadcrumb];
        }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to add breadcrumb: %@", exception.reason);
    }
}

void SentryNativeBridgeSetExtra(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    @try {
        [SentrySDK configureScope:^(SentryScope *scope) {
            if (value != NULL) {
                [scope setExtraValue:[NSString stringWithUTF8String:value]
                              forKey:[NSString stringWithUTF8String:key]];
            } else {
                [scope removeExtraForKey:[NSString stringWithUTF8String:key]];
            }
        }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to set extra: %@", exception.reason);
    }
}

void SentryNativeBridgeSetTag(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    @try {
        [SentrySDK configureScope:^(SentryScope *scope) {
            if (value != NULL) {
                [scope setTagValue:[NSString stringWithUTF8String:value]
                            forKey:[NSString stringWithUTF8String:key]];
            } else {
                [scope removeTagForKey:[NSString stringWithUTF8String:key]];
            }
        }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to set tag: %@", exception.reason);
    }
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    @try {
        [SentrySDK configureScope:^(
            SentryScope *scope) { [scope removeTagForKey:[NSString stringWithUTF8String:key]]; }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to unset tag: %@", exception.reason);
    }
}

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username)
{
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        return;
    }

    @try {
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
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to set user: %@", exception.reason);
    }
}

void SentryNativeBridgeUnsetUser()
{
    @try {
        [SentrySDK configureScope:^(SentryScope *scope) { [scope setUser:nil]; }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to unset user: %@", exception.reason);
    }
}

NS_ASSUME_NONNULL_END
