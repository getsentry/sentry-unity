extern "C" {
void throw_cpp()
{
    throw "exception";
}
}

#if defined(__ANDROID__)
// Diagnostic only: this file is compiled into libil2cpp.so via --additional-cpp, so the symbol
// bindings below are the ones il2cpp's Android stack walker (os/Android/StackTrace.cpp) sees.
#include <android/log.h>
#include <dlfcn.h>
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include <unwind.h>

#define DIAG(...) __android_log_print(ANDROID_LOG_INFO, "SentryDiag", __VA_ARGS__)

extern "C" char end;

static void diag_log_mapping(const char* label, uintptr_t addr)
{
    Dl_info info = {};
    int ok = dladdr(reinterpret_cast<void*>(addr), &info);
    char found[512] = "<not in /proc/self/maps>";
    FILE* maps = fopen("/proc/self/maps", "r");
    if (maps != NULL)
    {
        char line[512];
        while (fgets(line, sizeof(line), maps) != NULL)
        {
            unsigned long lo = 0, hi = 0;
            if (sscanf(line, "%lx-%lx", &lo, &hi) == 2 && addr >= lo && addr < hi)
            {
                line[strcspn(line, "\n")] = '\0';
                strncpy(found, line, sizeof(found) - 1);
                break;
            }
        }
        fclose(maps);
    }
    DIAG("%s=%p dladdr=%d fname=%s fbase=%p sname=%s maps=[%s]", label, reinterpret_cast<void*>(addr), ok,
        ok && info.dli_fname ? info.dli_fname : "-", ok ? info.dli_fbase : NULL,
        ok && info.dli_sname ? info.dli_sname : "-", found);
}

struct diag_walk
{
    int count;
};

static _Unwind_Reason_Code diag_walk_callback(struct _Unwind_Context* context, void* arg)
{
    diag_walk* walk = static_cast<diag_walk*>(arg);
    uintptr_t ip = _Unwind_GetIP(context);
    if (walk->count < 6)
    {
        char label[32];
        snprintf(label, sizeof(label), "frame[%d]", walk->count);
        diag_log_mapping(label, ip);
    }
    walk->count++;
    return walk->count < 64 ? _URC_NO_REASON : _URC_END_OF_STACK;
}

extern "C" void sentry_diag_unwind_bindings(const char* when)
{
    Dl_info self = {};
    dladdr(reinterpret_cast<void*>(&sentry_diag_unwind_bindings), &self);
    uintptr_t base = reinterpret_cast<uintptr_t>(self.dli_fbase);
    uintptr_t endAddr = reinterpret_cast<uintptr_t>(&end);
    DIAG("[%s] image=%s base=%p end=%p endAboveBase=%d", when, self.dli_fname ? self.dli_fname : "-",
        reinterpret_cast<void*>(base), reinterpret_cast<void*>(endAddr), endAddr > base ? 1 : 0);
    diag_log_mapping("end", endAddr);
    diag_log_mapping("_Unwind_Backtrace", reinterpret_cast<uintptr_t>(&_Unwind_Backtrace));
    diag_log_mapping("_Unwind_GetIP", reinterpret_cast<uintptr_t>(&_Unwind_GetIP));

    diag_walk walk = {0};
    _Unwind_Backtrace(diag_walk_callback, &walk);
    DIAG("[%s] _Unwind_Backtrace frames=%d", when, walk.count);
}
#endif
