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
        NSLog(@"Sentry (bridge): Couldn't load class '" #name "' from the dynamic library");       \
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
                NSLog(@"Sentry (bridge): Couldn't load Sentry.dylib - dlopen() failed");
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

void SentryConfigureScope(void (^callback)(id))
{
    // setValue:forKey: may throw if the property is not found; same for performSelector.
    // Even though this shouldn't happen, better not take the chance of letting an unhandled
    // exception while setting error info - it would just crash the app immediately.
    @try {
        [SentrySDK performSelector:@selector(configureScope:) withObject:callback];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to configure scope: %@", exception.reason);
    }
}

/*******************************************************************************/
/* The remaining code is a copy of iOS/SentryNativeBridge.m with changes to    */
/* make it work with dynamically loaded classes. Mainly:                       */
/*   - call: [class performSelector:@selector(arg1:arg2:)                      */
/*                  withObject:arg1Value withObject:arg2Value];                */
/*     or xCode warns of class/instance method not found                       */
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
        [SentrySDK performSelector:@selector(close)];
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

    SentryConfigureScope(^(id scope) {
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
    });
}

void SentryNativeBridgeSetExtra(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    SentryConfigureScope(^(id scope) {
        if (value != NULL) {
            [scope performSelector:@selector(setExtraValue:forKey:)
                        withObject:[NSString stringWithUTF8String:value]
                        withObject:[NSString stringWithUTF8String:key]];
        } else {
            [scope performSelector:@selector(removeExtraForKey:)
                        withObject:[NSString stringWithUTF8String:key]];
        }
    });
}

void SentryNativeBridgeSetTag(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    SentryConfigureScope(^(id scope) {
        if (value != NULL) {
            [scope performSelector:@selector(setTagValue:forKey:)
                        withObject:[NSString stringWithUTF8String:value]
                        withObject:[NSString stringWithUTF8String:key]];
        } else {
            [scope performSelector:@selector(removeTagForKey:)
                        withObject:[NSString stringWithUTF8String:key]];
        }
    });
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    SentryConfigureScope(^(id scope) {
        [scope performSelector:@selector(removeTagForKey:)
                    withObject:[NSString stringWithUTF8String:key]];
    });
}

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username)
{
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        return;
    }

    SentryConfigureScope(^(id scope) {
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

        [scope performSelector:@selector(setUser:) withObject:user];
    });
}

void SentryNativeBridgeUnsetUser()
{
    SentryConfigureScope(
        ^(id scope) { [scope performSelector:@selector(setUser:) withObject:nil]; });
}
