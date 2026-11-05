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

// Round 4: wrap the backtrace function Unity installs through il2cpp_override_stack_backtrace, so every
// il2cpp stack walk (including the one PrepareExceptionForThrow does at a throw) is logged with its result.
typedef void (*DiagMethodPointer)();
typedef size_t (*DiagBacktraceFunc)(DiagMethodPointer* buffer, size_t maxSize);
extern "C" void il2cpp_override_stack_backtrace(DiagBacktraceFunc stackBacktraceFunc);

static DiagBacktraceFunc s_diag_unity_backtrace;
static __thread char s_diag_phase[64];
static volatile int s_diag_fallback;
static int s_diag_calls;
static int s_diag_logged;

struct diag_fill
{
    DiagMethodPointer* buffer;
    size_t max;
    size_t count;
};

static _Unwind_Reason_Code diag_fill_callback(struct _Unwind_Context* context, void* arg)
{
    diag_fill* fill = static_cast<diag_fill*>(arg);
    if (fill->count >= fill->max)
        return _URC_END_OF_STACK;
    uintptr_t ip = _Unwind_GetIP(context);
    if (ip == 0)
        return _URC_END_OF_STACK;
    // Return addresses minus 1, as Unity's walker reports them, so each frame resolves to its call site.
    fill->buffer[fill->count++] = reinterpret_cast<DiagMethodPointer>(ip - 1);
    return _URC_NO_REASON;
}

static diag_il2cpp_walk diag_walk_from_buffer(DiagMethodPointer* buffer, size_t count)
{
    diag_il2cpp_walk walk = {};
    for (size_t i = 0; i < count; i++)
        diag_il2cpp_walk_callback(buffer[i], &walk);
    return walk;
}

// Round 5: Unity's backtrace function (libunity.so+0x2d0543 in 2021.3.45f2 x86_64) calls its stack walker
// (+0x2b8b56) as walker(callback, skip=1, context, 0); the callback gets (frame, context) with the pc at frame+8
// and keeps a frame only while the pc lies in [base, end] of the library that dlsym(handle, "end") resolved in
// (base at +0xd07390, end at +0xd07388) or dladdr names libunity.so. These offsets are only valid for that build.
static const uintptr_t kUnityBacktraceOffset = 0x2d0543;
static const uintptr_t kUnityWalkerOffset = 0x2b8b56;
static const uintptr_t kUnityEndOffset = 0xd07388;
static const uintptr_t kUnityBaseOffset = 0xd07390;
static uintptr_t s_diag_unity_base;

typedef bool (*DiagUnityWalkCallback)(void* frame, void* context);
typedef void (*DiagUnityWalker)(DiagUnityWalkCallback callback, int skip, void* context, int flags);

struct diag_unity_walk
{
    int frames;
    int accepted;
    bool stopped;
    char first[512];
    size_t used;
};

static bool diag_unity_filter(uintptr_t pc)
{
    uintptr_t filterEnd = *reinterpret_cast<uintptr_t*>(s_diag_unity_base + kUnityEndOffset);
    uintptr_t base = *reinterpret_cast<uintptr_t*>(s_diag_unity_base + kUnityBaseOffset);
    if (pc >= base && pc <= filterEnd)
        return true;
    Dl_info info = {};
    return dladdr(reinterpret_cast<void*>(pc), &info) && info.dli_fname != NULL && strstr(info.dli_fname, "libunity.so") != NULL;
}

static bool diag_unity_walk_callback(void* frame, void* context)
{
    diag_unity_walk* walk = static_cast<diag_unity_walk*>(context);
    const uintptr_t* words = static_cast<const uintptr_t*>(frame);
    uintptr_t pc = words[1];
    bool pass = diag_unity_filter(pc);
    if (walk->frames < 8 && walk->used < sizeof(walk->first))
    {
        Dl_info info = {};
        const char* lib = "?";
        if (dladdr(reinterpret_cast<void*>(pc), &info) && info.dli_fname != NULL)
        {
            const char* slash = strrchr(info.dli_fname, '/');
            lib = slash != NULL ? slash + 1 : info.dli_fname;
        }
        walk->used += snprintf(walk->first + walk->used, sizeof(walk->first) - walk->used, " [w0=%lx pc=%s+0x%lx w2=%lx %s]",
            (unsigned long)words[0], lib, (unsigned long)(pc - reinterpret_cast<uintptr_t>(info.dli_fbase)), (unsigned long)words[2],
            pass ? "ok" : "REJECT");
    }
    walk->frames++;
    if (pass)
        walk->accepted++;
    else if (!walk->stopped)
        walk->stopped = true;
    return walk->frames < 64;
}

static void diag_log_unity_walk(const char* label)
{
    if (s_diag_unity_base == 0)
        return;
    diag_unity_walk walk = {};
    reinterpret_cast<DiagUnityWalker>(s_diag_unity_base + kUnityWalkerOffset)(&diag_unity_walk_callback, 1, &walk, 0);
    DIAG("[%s] unity walker frames=%d filterAccepted=%d firstRejectSeen=%d:%s", label, walk.frames, walk.accepted,
        walk.stopped ? 1 : 0, walk.first);
}

static size_t diag_backtrace(DiagMethodPointer* buffer, size_t maxSize)
{
    size_t count = s_diag_unity_backtrace != NULL ? s_diag_unity_backtrace(buffer, maxSize) : 0;
    int call = __atomic_add_fetch(&s_diag_calls, 1, __ATOMIC_RELAXED);
    bool phased = s_diag_phase[0] != '\0';
    if ((!phased && count != 0) || __atomic_add_fetch(&s_diag_logged, 1, __ATOMIC_RELAXED) > 300)
        return count;

    char label[96];
    snprintf(label, sizeof(label), "%s call%d", phased ? s_diag_phase : "-", call);
    void* caller = __builtin_return_address(0);
    Dl_info callerInfo = {};
    dladdr(caller, &callerInfo);
    DIAG("[%s] unity backtrace returned %zu (max %zu) caller=+0x%lx", label, count, maxSize,
        (unsigned long)(reinterpret_cast<uintptr_t>(caller) - reinterpret_cast<uintptr_t>(callerInfo.dli_fbase)));
    diag_log_il2cpp_walk(label, "UnityBacktrace", diag_walk_from_buffer(buffer, count));
    if (count != 0)
        return count;

    diag_log_unity_walk(label);
    diag_fill fill = {buffer, maxSize, 0};
    _Unwind_Backtrace(diag_fill_callback, &fill);
    diag_log_il2cpp_walk(label, "UnwindBacktraceHere", diag_walk_from_buffer(buffer, fill.count));
    if (s_diag_fallback)
    {
        DIAG("[%s] fallback: returning the %zu _Unwind_Backtrace frames", label, fill.count);
        return fill.count;
    }
    return 0;
}

// WalkStack starts with `cmpq $0x0, s_StackBacktraceFunc(%rip)`; decode it to find the file-static slot.
static DiagBacktraceFunc* diag_find_backtrace_slot()
{
#if defined(__x86_64__)
    const unsigned char* code = reinterpret_cast<const unsigned char*>(&il2cpp::os::StackTrace::WalkStack);
    for (int i = 0; i < 96; i++)
    {
        if (code[i] == 0x48 && code[i + 1] == 0x83 && code[i + 2] == 0x3d && code[i + 7] == 0x00)
        {
            int32_t disp;
            memcpy(&disp, code + i + 3, sizeof(disp));
            return reinterpret_cast<DiagBacktraceFunc*>(const_cast<unsigned char*>(code + i + 8 + disp));
        }
    }
#endif
    return NULL;
}

extern "C" void sentry_diag_install_backtrace_wrapper(const char* when)
{
    DiagBacktraceFunc* slot = diag_find_backtrace_slot();
    if (slot == NULL)
    {
        DIAG("[%s] backtrace slot not found in WalkStack", when);
        return;
    }
    DiagBacktraceFunc current = *slot;
    Dl_info info = {};
    dladdr(reinterpret_cast<void*>(current), &info);
    DIAG("[%s] backtrace slot=%p current=%p lib=%s offset=0x%lx sname=%s", when, reinterpret_cast<void*>(slot),
        reinterpret_cast<void*>(current), info.dli_fname ? info.dli_fname : "-",
        (unsigned long)(reinterpret_cast<uintptr_t>(current) - reinterpret_cast<uintptr_t>(info.dli_fbase)),
        info.dli_sname ? info.dli_sname : "-");
    if (current == &diag_backtrace)
        return;
    uintptr_t unityBase = reinterpret_cast<uintptr_t>(info.dli_fbase);
    if (info.dli_fname != NULL && strstr(info.dli_fname, "libunity.so") != NULL
        && reinterpret_cast<uintptr_t>(current) - unityBase == kUnityBacktraceOffset)
    {
        s_diag_unity_base = unityBase;
        uintptr_t filterEnd = *reinterpret_cast<uintptr_t*>(unityBase + kUnityEndOffset);
        uintptr_t filterBase = *reinterpret_cast<uintptr_t*>(unityBase + kUnityBaseOffset);
        diag_log_mapping("unity filter end", filterEnd);
        diag_log_mapping("unity filter base", filterBase);
        DIAG("[%s] unity filter range=[%p, %p] il2cpp end=%p", when, reinterpret_cast<void*>(filterBase),
            reinterpret_cast<void*>(filterEnd), reinterpret_cast<void*>(&end));
    }
    else
    {
        DIAG("[%s] unity backtrace is not at the expected offset; walker logging disabled", when);
    }
    s_diag_unity_backtrace = current;
    il2cpp_override_stack_backtrace(&diag_backtrace);
    DIAG("[%s] backtrace wrapper installed=%d", when, *slot == &diag_backtrace ? 1 : 0);
}

extern "C" void sentry_diag_set_phase(const char* phase)
{
    if (phase == NULL)
        s_diag_phase[0] = '\0';
    else
        snprintf(s_diag_phase, sizeof(s_diag_phase), "%s", phase);
}

extern "C" void sentry_diag_set_fallback(int enabled)
{
    s_diag_fallback = enabled;
    DIAG("fallback to _Unwind_Backtrace on an empty Unity backtrace: %d", enabled);
}
#endif
