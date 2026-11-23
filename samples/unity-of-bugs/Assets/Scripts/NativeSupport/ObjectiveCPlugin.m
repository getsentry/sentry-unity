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

void applicationNotResponding()
{
    NSLog(@"Stalling the main thread from Objective-C to trigger a native ANR.");
    [NSThread sleepForTimeInterval:10.0]; // ANR detection currently defaults to 5 seconds
    NSLog(@"Objective-C main thread stall finished.");
}

// Returns the value that follows `--<name>` in the process arguments, or NULL. The copy is on the heap, as the
// marshaller expects: see Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
char *_Nullable getLaunchArgument(const char *name)
{
    NSString *flag = [NSString stringWithFormat:@"--%s", name];
    NSArray<NSString *> *args = NSProcessInfo.processInfo.arguments;
    for (NSUInteger i = 1; i + 1 < args.count; i++) {
        if ([args[i] isEqualToString:flag]) {
            return strdup([args[i + 1] UTF8String]);
        }
    }

    return NULL;
}

NS_ASSUME_NONNULL_END
