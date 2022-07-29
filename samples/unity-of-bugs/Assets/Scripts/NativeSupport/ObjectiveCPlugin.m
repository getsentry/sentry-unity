#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

void throwObjectiveC()
{
#ifdef __EXCEPTIONS
    NSLog(@"Throwing an Objective-C Exception");
    @throw [NSException exceptionWithName:@"Objective-C Exception"
                                   reason:@"Sentry Unity Objective-C Support."
                                 userInfo:nil];
#else
    NSLog(@"Objective-C Exceptions are disabled. "
           "Consider enabling it in the Xcode project: "
           "GCC_ENABLE_OBJC_EXCEPTIONS");
#endif
}

char *getTestArgObjectiveC()
{
    NSArray *args = NSProcessInfo.processInfo.arguments;

    // NSLog(@"getTestArgObjectiveC() args count = %lul", args.count);
    // for (int i = 0; i < args.count; i++) {
    //     NSLog(@"getTestArgObjectiveC() args[%d] = %@", i, args[i]);
    // }

    if (args.count < 3) {
        return NULL;
    }

    if (![args[1] isEqualToString:@"--test"]) {
        return NULL;
    }

    // create a null terminated C string on the heap as expected by marshalling
    // see Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
    const char *nsStringUtf8 = [args[2] UTF8String];
    size_t len = strlen(nsStringUtf8) + 1;
    char *cString = (char *)malloc(len);
    strcpy(cString, nsStringUtf8);
    cString[len - 1] = 0;

    return cString;
}

NS_ASSUME_NONNULL_END
