#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

// Raised rather than @throw-n: Unity's Xcode project builds with Objective-C exceptions off, which
// only the @ syntax needs.
void throwObjectiveC()
{
    NSLog(@"Throwing an Objective-C Exception");
    [[NSException exceptionWithName:@"Objective-C Exception"
                             reason:@"Sentry Unity Objective-C Support."
                           userInfo:nil] raise];
}

void applicationNotResponding()
{
    NSLog(@"Stalling the main thread from Objective-C to trigger a native ANR.");
    [NSThread sleepForTimeInterval:10.0]; // ANR detection currently defaults to 5 seconds
    NSLog(@"Objective-C main thread stall finished.");
}

NS_ASSUME_NONNULL_END
