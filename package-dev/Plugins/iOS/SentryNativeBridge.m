#import <Sentry/Sentry.h>

NS_ASSUME_NONNULL_BEGIN

// must be here to match the native bridge API for macOS
int SentryNativeBridgeInit() {
    return 0; // this shouldn't be used so return "false"
}

int SentryNativeBridgeCrashedLastRun() {
    return [SentrySDK crashedLastRun] ? 1 : 0;
}

void SentryNativeBridgeClose() {
    [SentrySDK close];
}

void SentryNativeBridgeAddBreadcrumb(const char* timestamp, const char* message, const char* type, const char* category, int level) {
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc] init];

        if (timestamp != NULL) {
            NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
            [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
            breadcrumb.timestamp = [dateFormatter dateFromString:[NSString stringWithCString:timestamp encoding:NSUTF8StringEncoding]];
        }

        if (message != NULL) {
            breadcrumb.message = [NSString stringWithCString:message encoding:NSUTF8StringEncoding];
        }

        if (type != NULL) {
            breadcrumb.type = [NSString stringWithCString:type encoding:NSUTF8StringEncoding];
        }

        if (category != NULL) {
            breadcrumb.category = [NSString stringWithCString:category encoding:NSUTF8StringEncoding];
        }

        breadcrumb.level = level;

        [scope addBreadcrumb:breadcrumb];
    }];
}

void SentryNativeBridgeSetExtra(const char* key, const char* value) {
    if (key == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        if (value != NULL) {
            [scope setExtraValue:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
        } else {
            [scope removeExtraForKey:[NSString stringWithUTF8String:key]];
        }
    }];
}

void SentryNativeBridgeSetTag(const char* key, const char* value) {
    if (key == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        if (value != NULL) {
            [scope setTagValue:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
        } else {
            [scope removeTagForKey:[NSString stringWithUTF8String:key]];
        }
    }];
}

void SentryNativeBridgeUnsetTag(const char* key) {
    if (key == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        [scope removeTagForKey:[NSString stringWithUTF8String:key]];
    }];
}

void SentryNativeBridgeSetUser(const char* email, const char* userId, const char* ipAddress, const char* username) {
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        SentryUser *user = [[SentryUser alloc] init];

        if (email != NULL) {
            user.email = [NSString stringWithCString:email encoding:NSUTF8StringEncoding];
        }

        if (userId != NULL) {
            user.userId = [NSString stringWithCString:userId encoding:NSUTF8StringEncoding];
        }

        if (ipAddress != NULL) {
            user.ipAddress = [NSString stringWithCString:ipAddress encoding:NSUTF8StringEncoding];
        }

        if (username != NULL) {
            user.username = [NSString stringWithCString:username encoding:NSUTF8StringEncoding];
        }

        [scope setUser:user];
    }];
}

void SentryNativeBridgeUnsetUser() {
    [SentrySDK configureScope:^(SentryScope * scope) {
        [scope setUser:nil];
    }];
}

NS_ASSUME_NONNULL_END
