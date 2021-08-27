#import <Sentry/Sentry.h>

void SentryNativeBridgeAddBreadcrumb(const char* timestamp, const char* message, const char* type, const char* category, int* level) {
    NSLog(@"Native Bridge: Adding breadcrumb.");
    
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL && level == NULL) {
        NSLog(@"Native Bridge: Breadcrumb empty. Dropping it.");
        return;
    }
    
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
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

void SentryNativeBridgeAddExtra(const char* key) {
    NSLog(@"Native Bridge: Adding extra. (just not yet)");
    //[SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
    //}];
}

void SentryNativeBridgeSetTag(const char* key, const char* value) {
    NSLog(@"Native Bridge: Setting tag.");
    
    if (key == NULL) {
        NSLog(@"Native Bridge: Tag key empty. Dropping it.");
        return;
    }
    
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        if (value != NULL) {
            [scope setTagValue:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
        }
        else {
            [scope removeTagForKey:[NSString stringWithUTF8String:key]];
        }
    }];
}

void SentryNativeBridgeUnsetTag(const char* key) {
    NSLog(@"Native Bridge: Unsetting tag.");
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        [scope removeTagForKey:[NSString stringWithUTF8String:key]];
    }];
}

void SentryNativeBridgeSetUser(const char* email, const char* userId, const char* ipAddress, const char* username) {
    NSLog(@"Native Bridge: Setting User.");
    
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        NSLog(@"Native Bridge: User empty. Dropping it.");
        return;
    }
    
    
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        SentryUser *user = [[SentryUser alloc] init];
        
        if (email != NULL) {
            user.email = [NSString stringWithCString:email encoding:NSUTF8StringEncoding];
        }
        
        if (email != NULL) {
            user.userId = [NSString stringWithCString:userId encoding:NSUTF8StringEncoding];
        }
        
        if (email != NULL) {
            user.ipAddress = [NSString stringWithCString:ipAddress encoding:NSUTF8StringEncoding];
        }
        
        if (email != NULL) {
            user.username = [NSString stringWithCString:username encoding:NSUTF8StringEncoding];
        }

        [scope setUser:user];
    }];
}

void SentryNativeBridgeUnsetUser()
{
    NSLog(@"Native Bridge: Unsetting user.");
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        [scope setUser:nil];
    }];
}
