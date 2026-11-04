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
extern "C" char __start_il2cpp;
extern "C" char __stop_il2cpp;

// il2cpp's own checks used by its Android stack walker (vm/StackTrace.cpp, vm-utils/NativeSymbol.cpp).
struct MethodInfo;
namespace il2cpp
{
namespace os
{
namespace Image
{
    bool ManagedSectionExists();
    bool IsInManagedSection(void* ip);
}
}
namespace utils
{
    class NativeSymbol
    {
    public:
        static const MethodInfo* GetMethodFromNativeSymbol(void (*nativeMethod)());
    };
}
}

namespace il2cpp
{
namespace os
{
    typedef bool (*WalkStackCallback)(void (*frame)(), void* context);
    // WalkStack goes through an installed il2cpp_override_stack_backtrace function if there is one,
    // WalkStackNative is the _Unwind_Backtrace walker (declared public here to call it directly).
    class StackTrace
    {
    public:
        enum WalkOrder
        {
            kFirstCalledToLastCalled,
            kLastCalledToFirstCalled
        };
        static void WalkStack(WalkStackCallback callback, void* context, WalkOrder walkOrder);
        static void WalkStackNative(WalkStackCallback callback, void* context, WalkOrder walkOrder);
    };
}
}

struct Il2CppObject;
extern "C" Il2CppObject* il2cpp_gchandle_get_target(uint32_t gchandle);
extern "C" void il2cpp_native_stack_trace(const void* ex, uintptr_t** addresses, int* numFrames, char** imageUUID, char** imageName);
extern "C" void il2cpp_free(void* ptr);

static uintptr_t s_diag_base;

struct diag_il2cpp_walk
{
    int frames;
    int managed;
    uintptr_t first[6];
};

static bool diag_il2cpp_walk_callback(void (*frame)(), void* context)
{
    diag_il2cpp_walk* walk = static_cast<diag_il2cpp_walk*>(context);
    if (walk->frames < 6)
        walk->first[walk->frames] = reinterpret_cast<uintptr_t>(frame);
    walk->frames++;
    if (il2cpp::utils::NativeSymbol::GetMethodFromNativeSymbol(frame) != NULL)
        walk->managed++;
    return true;
}

static void diag_log_il2cpp_walk(const char* when, const char* which, const diag_il2cpp_walk& walk)
{
    char offsets[256] = "";
    size_t used = 0;
    for (int i = 0; i < walk.frames && i < 6 && used < sizeof(offsets); i++)
    {
        uintptr_t addr = walk.first[i];
        Dl_info info = {};
        const char* lib = "?";
        if (dladdr(reinterpret_cast<void*>(addr), &info) && info.dli_fname != NULL)
        {
            const char* slash = strrchr(info.dli_fname, '/');
            lib = slash != NULL ? slash + 1 : info.dli_fname;
        }
        used += snprintf(offsets + used, sizeof(offsets) - used, " %s+0x%lx", lib,
            (unsigned long)(addr - reinterpret_cast<uintptr_t>(info.dli_fbase)));
    }
    DIAG("[%s] %s frames=%d managed=%d first:%s", when, which, walk.frames, walk.managed, offsets);
}

extern "C" void sentry_diag_compare_walks(const char* when)
{
    diag_il2cpp_walk viaWalkStack = {};
    il2cpp::os::StackTrace::WalkStack(diag_il2cpp_walk_callback, &viaWalkStack, il2cpp::os::StackTrace::kLastCalledToFirstCalled);
    diag_il2cpp_walk viaNative = {};
    il2cpp::os::StackTrace::WalkStackNative(diag_il2cpp_walk_callback, &viaNative, il2cpp::os::StackTrace::kLastCalledToFirstCalled);
    diag_log_il2cpp_walk(when, "WalkStack(override-or-native)", viaWalkStack);
    diag_log_il2cpp_walk(when, "WalkStackNative(_Unwind_Backtrace)", viaNative);
}

extern "C" void sentry_diag_exception_trace(uintptr_t gchandle, const char* when)
{
    Il2CppObject* ex = il2cpp_gchandle_get_target(static_cast<uint32_t>(gchandle));
    uintptr_t* addresses = NULL;
    int numFrames = 0;
    char* uuid = NULL;
    char* name = NULL;
    il2cpp_native_stack_trace(ex, &addresses, &numFrames, &uuid, &name);
    DIAG("[%s] exception native frames=%d image=%s", when, numFrames, name != NULL ? name : "-");
    if (addresses != NULL)
        il2cpp_free(addresses);
    if (uuid != NULL)
        il2cpp_free(uuid);
    if (name != NULL)
        il2cpp_free(name);
}

static void diag_log_guest_cpu()
{
    FILE* cpuinfo = fopen("/proc/cpuinfo", "r");
    if (cpuinfo == NULL)
        return;
    char line[2048];
    int logged = 0;
    while (fgets(line, sizeof(line), cpuinfo) != NULL && logged < 6)
    {
        if (strncmp(line, "vendor_id", 9) == 0 || strncmp(line, "model name", 10) == 0 || strncmp(line, "cpu family", 10) == 0
            || strncmp(line, "model\t", 6) == 0 || strncmp(line, "flags", 5) == 0 || strncmp(line, "microcode", 9) == 0)
        {
            line[strcspn(line, "\n")] = '\0';
            DIAG("guest cpuinfo: %s", line);
            logged++;
        }
    }
    fclose(cpuinfo);
}

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
    if (walk->count < 8)
    {
        char label[32];
        snprintf(label, sizeof(label), "frame[%d]", walk->count);
        diag_log_mapping(label, ip);
        DIAG("%s offset=0x%lx inManagedSection=%d method=%p", label, (unsigned long)(ip - s_diag_base),
            il2cpp::os::Image::IsInManagedSection(reinterpret_cast<void*>(ip)) ? 1 : 0,
            reinterpret_cast<const void*>(il2cpp::utils::NativeSymbol::GetMethodFromNativeSymbol(reinterpret_cast<void (*)()>(ip))));
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
    s_diag_base = base;
    diag_log_guest_cpu();
    diag_log_mapping("end", endAddr);
    DIAG("[%s] managedSectionExists=%d", when, il2cpp::os::Image::ManagedSectionExists() ? 1 : 0);
    diag_log_mapping("__start_il2cpp", reinterpret_cast<uintptr_t>(&__start_il2cpp));
    diag_log_mapping("__stop_il2cpp", reinterpret_cast<uintptr_t>(&__stop_il2cpp));
    diag_log_mapping("_Unwind_Backtrace", reinterpret_cast<uintptr_t>(&_Unwind_Backtrace));
    diag_log_mapping("_Unwind_GetIP", reinterpret_cast<uintptr_t>(&_Unwind_GetIP));

    diag_walk walk = {0};
    _Unwind_Backtrace(diag_walk_callback, &walk);
    DIAG("[%s] _Unwind_Backtrace frames=%d", when, walk.count);
}
#endif
