#import <Foundation/Foundation.h>
#include <dlfcn.h>

static int loadStatus = -1; // unitialized

static Class SentrySDK;
static Class SentryScope;
static Class SentryBreadcrumb;

#define LOAD_CLASS_OR_BREAK(name)                                                                  \
    name = dlsym(dylib, "OBJC_CLASS_$_" #name);                                                    \
    if (!name) {                                                                                   \
        NSLog(@("Couldn't load " #name " class from the dynamic library"));                        \
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
                NSLog(@"Couldn't load Sentry.dylib - dlopen() failed");
                break;
            }

            LOAD_CLASS_OR_BREAK(SentrySDK)
            LOAD_CLASS_OR_BREAK(SentryScope)
            LOAD_CLASS_OR_BREAK(SentryBreadcrumb)

            // everything above passed - mark as successfully loaded
            loadStatus = 1;
        } while (false);
    }
    return loadStatus;
}

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

/******************************************************************************************/
/* THE REMAINING CODE IS A LITERAL COPY OF THE iOS/SentryNativeBridge.m                   */
/* Note: maybe we could avoid the code copy, e.g. by doing the dlopen(__internal) on iOS? */
/******************************************************************************************/

int SentryNativeBridgeCrashedLastRun() { return [SentrySDK crashedLastRun] ? 1 : 0; }

void SentryNativeBridgeClose() { [SentrySDK close]; }

void SentryNativeBridgeAddBreadcrumb(
    const char *timestamp, const char *message, const char *type, const char *category, int level)
{
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    // [SentrySDK configureScope:^(SentryScope *scope) {
    //     SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc] init];

    //     if (timestamp != NULL) {
    //         NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
    //         [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
    //         breadcrumb.timestamp =
    //             [dateFormatter dateFromString:[NSString stringWithCString:timestamp
    //                                                              encoding:NSUTF8StringEncoding]];
    //     }

    //     if (message != NULL) {
    //         breadcrumb.message = [NSString stringWithCString:message
    //         encoding:NSUTF8StringEncoding];
    //     }

    //     if (type != NULL) {
    //         breadcrumb.type = [NSString stringWithCString:type encoding:NSUTF8StringEncoding];
    //     }

    //     if (category != NULL) {
    //         breadcrumb.category = [NSString stringWithCString:category
    //                                                  encoding:NSUTF8StringEncoding];
    //     }

    //     breadcrumb.level = level;

    //     [scope addBreadcrumb:breadcrumb];
    // }];
}

void SentryNativeBridgeSetExtra(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    // [SentrySDK configureScope:^(SentryScope *scope) {
    //     if (value != NULL) {
    //         [scope setExtraValue:[NSString stringWithUTF8String:value]
    //                       forKey:[NSString stringWithUTF8String:key]];
    //     } else {
    //         [scope removeExtraForKey:[NSString stringWithUTF8String:key]];
    //     }
    // }];
}

void SentryNativeBridgeSetTag(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    // [SentrySDK configureScope:^(SentryScope *scope) {
    //     if (value != NULL) {
    //         [scope setTagValue:[NSString stringWithUTF8String:value]
    //                     forKey:[NSString stringWithUTF8String:key]];
    //     } else {
    //         [scope removeTagForKey:[NSString stringWithUTF8String:key]];
    //     }
    // }];
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    // [SentrySDK configureScope:^(
    //     SentryScope *scope) { [scope removeTagForKey:[NSString stringWithUTF8String:key]]; }];
}

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username)
{
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        return;
    }

    // [SentrySDK configureScope:^(SentryScope *scope) {
    //     SentryUser *user = [[SentryUser alloc] init];

    //     if (email != NULL) {
    //         user.email = [NSString stringWithCString:email encoding:NSUTF8StringEncoding];
    //     }

    //     if (userId != NULL) {
    //         user.userId = [NSString stringWithCString:userId encoding:NSUTF8StringEncoding];
    //     }

    //     if (ipAddress != NULL) {
    //         user.ipAddress = [NSString stringWithCString:ipAddress
    //         encoding:NSUTF8StringEncoding];
    //     }

    //     if (username != NULL) {
    //         user.username = [NSString stringWithCString:username encoding:NSUTF8StringEncoding];
    //     }

    //     [scope setUser:user];
    // }];
}

void SentryNativeBridgeUnsetUser()
{
    // [SentrySDK configureScope:^(SentryScope *scope) { [scope setUser:nil]; }];
}
