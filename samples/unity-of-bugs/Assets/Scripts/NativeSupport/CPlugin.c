#ifdef _WIN32
#    define NOINLINE __declspec(noinline)
#else
#    define NOINLINE __attribute__((noinline))
#endif

NOINLINE void crash_in_c()
{
    char *ptr = 0;
    *ptr += 1;
}

typedef void (*callback_t)(int code);

NOINLINE void call_into_csharp(callback_t fn) { fn(42); }
