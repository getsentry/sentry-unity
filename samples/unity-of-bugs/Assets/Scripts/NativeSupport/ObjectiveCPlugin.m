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

NS_ASSUME_NONNULL_END
