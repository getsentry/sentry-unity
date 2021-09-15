#import <Sentry/Sentry.h>
#import <Sentry/SentryLog.h>

NS_ASSUME_NONNULL_BEGIN

void SentryNativeBridgeAddBreadcrumb(const char* timestamp, const char* message, const char* type, const char* category, int* level) {
    [SentryLog logWithMessage: @"Sentry Native Bridge: Adding breadcrumb" andLevel:kSentryLevelDebug];

    if (timestamp == NULL && message == NULL && type == NULL && category == NULL && level == NULL) {
        [SentryLog logWithMessage: @"Sentry Native Bridge: Breadcrumb empty. Can't add it" andLevel:kSentryLevelDebug];
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

        if (level != NULL) {
            breadcrumb.level = level;
        }

        [scope addBreadcrumb:breadcrumb];
    }];
}

void SentryNativeBridgeSetExtra(const char* key, const char* value) {
    [SentryLog logWithMessage: @"Sentry Native Bridge: Adding extra" andLevel:kSentryLevelDebug];

    if (key == NULL) {
        [SentryLog logWithMessage: @"Sentry Native Bridge: Extra key empty. Can't add it" andLevel:kSentryLevelDebug];
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
    [SentryLog logWithMessage: @"Sentry Native Bridge: Adding tag." andLevel:kSentryLevelDebug];

    if (key == NULL) {
        [SentryLog logWithMessage: @"Sentry Native Bridge: Tag key empty. Can't add it." andLevel:kSentryLevelDebug];
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        if (value != NULL) {
            [scope setTagValue:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
        } else {
            [SentryLog logWithMessage: @"Sentry Native Bridge: Tag value empty. Removing tag for key." andLevel:kSentryLevelDebug];
            [scope removeTagForKey:[NSString stringWithUTF8String:key]];
        }
    }];
}

void SentryNativeBridgeUnsetTag(const char* key) {
    [SentryLog logWithMessage: @"Sentry Native Bridge: Unsetting tag" andLevel:kSentryLevelDebug];

    if (key == NULL) {
        [SentryLog logWithMessage: @"Sentry Native Bridge: Tag key empty. Can't unset it" andLevel:kSentryLevelDebug];
        return;
    }

    [SentrySDK configureScope:^(SentryScope * scope) {
        [scope removeTagForKey:[NSString stringWithUTF8String:key]];
    }];
}

void SentryNativeBridgeSetUser(const char* email, const char* userId, const char* ipAddress, const char* username) {
    [SentryLog logWithMessage: @"Sentry Native Bridge: Setting user" andLevel:kSentryLevelDebug];

    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        [SentryLog logWithMessage: @"Sentry Native Bridge: User empty. Can't set it" andLevel:kSentryLevelDebug];
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
    [SentryLog logWithMessage: @"Sentry Native Bridge: Unsetting user" andLevel:kSentryLevelDebug];
    [SentrySDK configureScope:^(SentryScope * scope) {
        [scope setUser:nil];
    }];
}

NS_ASSUME_NONNULL_END
