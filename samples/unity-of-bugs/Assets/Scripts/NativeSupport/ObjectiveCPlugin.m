#import <Foundation/Foundation.h>
#import <mach/mach.h>
#import <sys/resource.h>

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

// The value after `-<name>` in the process's launch arguments, which IL2CPP's Environment.GetCommandLineArgs()
// doesn't return on iOS. The caller owns the returned string.
char *_Nullable sampleLaunchArgument(const char *name)
{
    NSString *flag = [@"-" stringByAppendingString:[NSString stringWithUTF8String:name]];
    NSArray<NSString *> *arguments = NSProcessInfo.processInfo.arguments;
    NSUInteger index = [arguments indexOfObject:flag];
    if (index == NSNotFound || index + 1 >= arguments.count) {
        return NULL;
    }
    return strdup(arguments[index + 1].UTF8String);
}

// The CPU time the process used so far, in seconds, on all cores. For ReplayBenchmark.
double sampleProcessCpuSeconds(void)
{
    struct rusage usage;
    if (getrusage(RUSAGE_SELF, &usage) != 0) {
        return -1;
    }
    return usage.ru_utime.tv_sec + usage.ru_utime.tv_usec / 1e6 + usage.ru_stime.tv_sec
        + usage.ru_stime.tv_usec / 1e6;
}

// The process's memory footprint in bytes, as Xcode's memory gauge and the system's memory limit count it.
// For ReplayBenchmark.
int64_t sampleMemoryFootprint(void)
{
    task_vm_info_data_t info;
    mach_msg_type_number_t count = TASK_VM_INFO_COUNT;
    if (task_info(mach_task_self(), TASK_VM_INFO, (task_info_t)&info, &count) != KERN_SUCCESS) {
        return -1;
    }
    return (int64_t)info.phys_footprint;
}

NS_ASSUME_NONNULL_END
