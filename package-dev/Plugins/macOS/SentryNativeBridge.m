#import <Foundation/Foundation.h>
#include <dlfcn.h>

static int loadStatus = -1; // unitialized

static Class SentrySDK;
static Class SentryScope;
static Class SentryBreadcrumb;
static Class SentryUser;

#define LOAD_CLASS_OR_BREAK(name)                                                                  \
    name = (__bridge Class)dlsym(dylib, "OBJC_CLASS_$_" #name);                                    \
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

const void *SentryNativeBridgeOptionsNew()
{
    return CFBridgingRetain([[NSMutableDictionary alloc] init]);
}

void SentryNativeBridgeOptionsSetString(const void *options, const char *name, const char *value)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSString stringWithUTF8String:value];
}

void SentryNativeBridgeOptionsSetInt(const void *options, const char *name, int32_t value)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSNumber numberWithInt:value];
}

void SentryNativeBridgeStartWithOptions(const void *options)
{
    NSMutableDictionary *dictOptions = (__bridge_transfer NSMutableDictionary *)options;
    [SentrySDK performSelector:@selector(startWithOptions:) withObject:dictOptions];
}

/*******************************************************************************/
/* The remaining code is a copy of iOS/SentryNativeBridge.m with changes to    */
/* make it work with dynamically loaded classes. Mainly:                       */
/*   - call: [class performSelector:@selector(arg1:arg2:)                      */
/*                  withObject:arg1Value withObject:arg2Value];                */
/*   - use `id` as variable types                                              */
/*   - use [obj setValue:value forKey:@"prop"] instead of `obj.prop = value`   */
/*******************************************************************************/

int SentryNativeBridgeCrashedLastRun()
{
    @try {
        return [SentrySDK performSelector:@selector(crashedLastRun)] ? 1 : 0;
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

    // declaring the (block) callback as a variable to avoid too much editor blank space on the left
    void (^scopeUpdateBlock)(id) = ^void(id scope) {
        id breadcrumb = [[SentryBreadcrumb alloc] init];

        if (timestamp != NULL) {
            NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
            [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
            [breadcrumb
                setValue:[dateFormatter dateFromString:[NSString stringWithUTF8String:timestamp]]
                  forKey:@"timestamp"];
        }

        if (message != NULL) {
            [breadcrumb setValue:[NSString stringWithUTF8String:message] forKey:@"message"];
        }

        if (type != NULL) {
            [breadcrumb setValue:[NSString stringWithUTF8String:type] forKey:@"type"];
        }

        if (category != NULL) {
            [breadcrumb setValue:[NSString stringWithUTF8String:category] forKey:@"category"];
        }

        [breadcrumb setValue:[NSNumber numberWithInt:level] forKey:@"level"];

        [scope performSelector:@selector(addBreadcrumb:) withObject:breadcrumb];
    };

    @try {
        [SentrySDK performSelector:@selector(configureScope:) withObject:scopeUpdateBlock];
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
        [SentrySDK performSelector:@selector(configureScope:)
                        withObject:^(id scope) {
                            if (value != NULL) {
                                [scope performSelector:@selector(setExtraValue:forKey:)
                                            withObject:[NSString stringWithUTF8String:value]
                                            withObject:[NSString stringWithUTF8String:key]];
                            } else {
                                [scope performSelector:@selector(removeExtraForKey:)
                                            withObject:[NSString stringWithUTF8String:key]];
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
        [SentrySDK performSelector:@selector(configureScope:)
                        withObject:^(id scope) {
                            if (value != NULL) {
                                [scope performSelector:@selector(setTagValue:forKey:)
                                            withObject:[NSString stringWithUTF8String:value]
                                            withObject:[NSString stringWithUTF8String:key]];
                            } else {
                                [scope performSelector:@selector(removeTagForKey:)
                                            withObject:[NSString stringWithUTF8String:key]];
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
        [SentrySDK performSelector:@selector(configureScope:)
                        withObject:^(id scope) {
                            [scope performSelector:@selector(removeTagForKey:)
                                        withObject:[NSString stringWithUTF8String:key]];
                        }];
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
        [SentrySDK
            performSelector:@selector(configureScope:)
                 withObject:^(id scope) {
                     id user = [[SentryUser alloc] init];

                     if (email != NULL) {
                         [user setValue:[NSString stringWithUTF8String:email] forKey:@"email"];
                     }

                     if (userId != NULL) {
                         [user setValue:[NSString stringWithUTF8String:userId] forKey:@"userId"];
                     }

                     if (ipAddress != NULL) {
                         [user setValue:[NSString stringWithUTF8String:ipAddress]
                                 forKey:@"ipAddress"];
                     }

                     if (username != NULL) {
                         [user setValue:[NSString stringWithUTF8String:username]
                                 forKey:@"username"];
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
        [SentrySDK performSelector:@selector(configureScope:)
                        withObject:^(id scope) { [scope setUser:nil]; }];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to unset user: %@", exception.reason);
    }
}
