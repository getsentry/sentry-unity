#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

void throwObjectiveC() {
    NSLog(@"Throwing an Objective-C Exception");

    @throw [NSException
                   exceptionWithName:@"Objective-C Exception"
                   reason:@"Sentry Unity Objective-C Support."
                   userInfo:nil];
}

NS_ASSUME_NONNULL_END
