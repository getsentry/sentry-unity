#import <Sentry/Sentry.h>

void SentryNativeBridgeAddBreadcrumb(const char* timestamp, const char* message, const char* type, const char* category, int* level)
{
    NSLog(@"Native Bridge: Adding breadcrumb.");
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc] init];
        
        NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
        [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
        breadcrumb.timestamp = [dateFormatter dateFromString:[NSString stringWithCString:timestamp encoding:NSUTF8StringEncoding]];
        
        breadcrumb.message = [NSString stringWithCString:message encoding:NSUTF8StringEncoding];
        breadcrumb.type = [NSString stringWithCString:type encoding:NSUTF8StringEncoding];
        breadcrumb.category = [NSString stringWithCString:category encoding:NSUTF8StringEncoding];
        breadcrumb.level = level;
        
        [scope addBreadcrumb:breadcrumb];
    }];
}

void SentryNativeBridgeAddExtra(const char* key)
{
    NSLog(@"Native Bridge: Adding extra. (just not yet)");
    //[SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
    //}];
}

void SentryNativeBridgeSetTag(const char* key, const char* value)
{
    NSLog(@"Native Bridge: Setting tag.");
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        [scope setTagValue:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
    }];
}

void SentryNativeBridgeUnsetTag(const char* key)
{
    NSLog(@"Native Bridge: Unsetting tag.");
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        [scope removeTagForKey:[NSString stringWithUTF8String:key]];
    }];
}

void SentryNativeBridgeSetUser(const char* email, const char* userId, const char* ipAddress, const char* username)
{
    NSLog(@"Native Bridge: Setting User.");
    [SentrySDK configureScope:^(SentryScope * _Nonnull scope) {
        SentryUser *user = [[SentryUser alloc] init];
        
        user.email = [NSString stringWithCString:email encoding:NSUTF8StringEncoding];
        user.userId = [NSString stringWithCString:userId encoding:NSUTF8StringEncoding];
        user.ipAddress = [NSString stringWithCString:ipAddress encoding:NSUTF8StringEncoding];
        user.username = [NSString stringWithCString:username encoding:NSUTF8StringEncoding];

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
