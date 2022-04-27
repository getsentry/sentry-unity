#import <Foundation/Foundation.h>
#include <dlfcn.h>

@class SentryOptions;
@class SentrySDK;

static int loadStatus = 0; // 0 = unitialized; 1 = dylib loaded successfully; -1 = dylib load error
static void *dylib;
static Class sdkClass;
static Class optionsClass;

int
LoadSentryDylib()
{
    if (!loadStatus) {
        loadStatus = -1; // init to "error"
        do {
            dylib = dlopen("@executable_path/../PlugIns/Sentry.dylib", RTLD_LAZY);
            if (!dylib) {
                NSLog(@"Couldn't load Sentry.dylib - dlopen() failed");
                break;
            }

            sdkClass = dlsym(dylib, "OBJC_CLASS_$_SentrySDK");
            if (!sdkClass) {
                NSLog(@"Couldn't load SentrySDK class from the dynamic library");
                break;
            }

            optionsClass = dlsym(dylib, "OBJC_CLASS_$_SentryOptions");
            if (!optionsClass) {
                NSLog(@"Couldn't load SentryOptions class from the dynamic library");
                break;
            }

            // everything above passed succesfully - mark as loaded
            loadStatus = 1;
        } while (false);
    }
    return loadStatus;
}

// TODO expose options setup & init
// SentryOptions *options = [[optionsClass alloc] init];
// [options
//     setValue:
//         @"https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417"
//       forKey:@"dsn"];
// [options setValue:[NSNumber numberWithBool:YES] forKey:@"debug"];

// [sdkClass startWithOptionsObject:options];

int CrashedLastRun() {
//     return [SentrySDK crashedLastRun] ? 1 : 0;
}

void Close() {
//     [SentrySDK close];
}

void SentryNativeBridgeAddBreadcrumb(const char* timestamp, const char* message, const char*
type, const char* category, int level) {
//     if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
//         return;
//     }

//     [SentrySDK configureScope:^(SentryScope * scope) {
//         SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc] init];

//         if (timestamp != NULL) {
//             NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
//             [dateFormatter setDateFormat:NSCalendarIdentifierISO8601];
//             breadcrumb.timestamp = [dateFormatter dateFromString:[NSString
//             stringWithCString:timestamp encoding:NSUTF8StringEncoding]];
//         }

//         if (message != NULL) {
//             breadcrumb.message = [NSString stringWithCString:message
//             encoding:NSUTF8StringEncoding];
//         }

//         if (type != NULL) {
//             breadcrumb.type = [NSString stringWithCString:type
//             encoding:NSUTF8StringEncoding];
//         }

//         if (category != NULL) {
//             breadcrumb.category = [NSString stringWithCString:category
//             encoding:NSUTF8StringEncoding];
//         }

//         breadcrumb.level = level;

//         [scope addBreadcrumb:breadcrumb];
//     }];
}

void SentryNativeBridgeSetExtra(const char* key, const char* value) {
//     if (key == NULL) {
//         return;
//     }

//     [SentrySDK configureScope:^(SentryScope * scope) {
//         if (value != NULL) {
//             [scope setExtraValue:[NSString stringWithUTF8String:value] forKey:[NSString
//             stringWithUTF8String:key]];
//         } else {
//             [scope removeExtraForKey:[NSString stringWithUTF8String:key]];
//         }
//     }];
}

void SentryNativeBridgeSetTag(const char* key, const char* value) {
//     if (key == NULL) {
//         return;
//     }

//     [SentrySDK configureScope:^(SentryScope * scope) {
//         if (value != NULL) {
//             [scope setTagValue:[NSString stringWithUTF8String:value] forKey:[NSString
//             stringWithUTF8String:key]];
//         } else {
//             [scope removeTagForKey:[NSString stringWithUTF8String:key]];
//         }
//     }];
}

void SentryNativeBridgeUnsetTag(const char* key) {
//     if (key == NULL) {
//         return;
//     }

//     [SentrySDK configureScope:^(SentryScope * scope) {
//         [scope removeTagForKey:[NSString stringWithUTF8String:key]];
//     }];
}

void SentryNativeBridgeSetUser(const char* email, const char* userId, const char* ipAddress,
const char* username) {
//     if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
//         return;
//     }

//     [SentrySDK configureScope:^(SentryScope * scope) {
//         SentryUser *user = [[SentryUser alloc] init];

//         if (email != NULL) {
//             user.email = [NSString stringWithCString:email encoding:NSUTF8StringEncoding];
//         }

//         if (userId != NULL) {
//             user.userId = [NSString stringWithCString:userId encoding:NSUTF8StringEncoding];
//         }

//         if (ipAddress != NULL) {
//             user.ipAddress = [NSString stringWithCString:ipAddress
//             encoding:NSUTF8StringEncoding];
//         }

//         if (username != NULL) {
//             user.username = [NSString stringWithCString:username
//             encoding:NSUTF8StringEncoding];
//         }

//         [scope setUser:user];
//     }];
}

void SentryNativeBridgeUnsetUser() {
//     [SentrySDK configureScope:^(SentryScope * scope) {
//         [scope setUser:nil];
//     }];
}
