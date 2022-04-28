#import <Foundation/Foundation.h>
#include <dlfcn.h>

static int loadStatus = -1; // unitialized

static Class SentrySDK;
static Class SentryScope;
static Class SentryBreadcrumb;
static Class SentryUser;

#define LOAD_CLASS_OR_BREAK(name)                                                                  \
    name = dlsym(dylib, "OBJC_CLASS_$_" #name);                                                    \
    if (!name) {                                                                                   \
        NSLog(@"Sentry (native bridge): Couldn't load %@ class from the dynamic library", name);   \
        break;                                                                                     \
    }

// Returns (bool): 0 on failure, 1 on success
// WARNING: you may only call other Sentry* functions AFTER calling this AND only if it returned "1"
int SentryNativeBridgeLoadLibrary()
{
    if (loadStatus == -1) {
        loadStatus = 0; // init to "error"
        do {
            void *dylib = dlopen("@executable_path/../PlugIns/Sentry.dylib", RTLD_LAZY);
            if (!dylib) {
                NSLog(@"Sentry (native bridge): Couldn't load Sentry.dylib - dlopen() failed");
                break;
            }

            LOAD_CLASS_OR_BREAK(SentrySDK)
            LOAD_CLASS_OR_BREAK(SentryScope)
            LOAD_CLASS_OR_BREAK(SentryBreadcrumb)
            LOAD_CLASS_OR_BREAK(SentryUser)

            // everything above passed - mark as successfully loaded
            loadStatus = 1;
        } while (false);
    }
    return loadStatus;
}

// We're doing dynamic access so we're getting warnings about missing class/instance methods...
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wobjc-method-access"

void *SentryNativeBridgeOptionsNew() { return (void *)[[NSMutableDictionary alloc] init]; }

void SentryNativeBridgeOptionsSetString(void *options, const char *name, const char *value)
{
    NSMutableDictionary *dictOptions = (NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSString stringWithUTF8String:value];
}

void SentryNativeBridgeOptionsSetInt(void *options, const char *name, int32_t value)
{
    NSMutableDictionary *dictOptions = (NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSNumber numberWithInt:value];
}

void SentryNativeBridgeStartWithOptions(void *options)
{
    [SentrySDK startWithOptions:((NSMutableDictionary *)options)];
    [((NSMutableDictionary *)options) release];
}

/*****************************************************************************/
/* The remaining code is a copy of iOS/SentryNativeBridge.m                  */
/* with minor changes to make it work with dynamically loaded classes.       */
/* Specifically:                                                             */
/*   - use `id` as variable types                                            */
/*   - use [obj setValue:value forKey:@"prop"] instead of `obj.prop = value` */
/*****************************************************************************/

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
        [SentrySDK configureScope:^(id scope) {
            id breadcrumb = [[SentryBreadcrumb alloc]
                initWithLevel:level
                     category:(category ? [NSString stringWithUTF8String:category] : nil)];

            if (timestamp != NULL) {
                NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
                [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
                [breadcrumb setValue:[dateFormatter
                                         dateFromString:[NSString stringWithUTF8String:timestamp]]
                              forKey:@"timestamp"];
                [dateFormatter release];
            }

            if (message != NULL) {
                [breadcrumb setValue:[NSString stringWithUTF8String:message] forKey:@"message"];
            }

            if (type != NULL) {
                [breadcrumb setValue:[NSString stringWithUTF8String:type] forKey:@"type"];
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
        [SentrySDK configureScope:^(id scope) {
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
        [SentrySDK configureScope:^(id scope) {
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
            id scope) { [scope removeTagForKey:[NSString stringWithUTF8String:key]]; }];
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
        [SentrySDK configureScope:^(id scope) {
            id user = [[SentryUser alloc] init];

            if (email != NULL) {
                [user setValue:[NSString stringWithUTF8String:email] forKey:@"email"];
            }

            if (userId != NULL) {
                [user setValue:[NSString stringWithUTF8String:userId] forKey:@"userId"];
            }

            if (ipAddress != NULL) {
                [user setValue:[NSString stringWithUTF8String:ipAddress] forKey:@"ipAddress"];
            }

            if (username != NULL) {
                [user setValue:[NSString stringWithUTF8String:username] forKey:@"username"];
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
        [SentrySDK configureScope:^(id scope) { [scope setUser:nil]; }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to unset user: %@", exception.reason);
    }
}

#pragma clang diagnostic pop
