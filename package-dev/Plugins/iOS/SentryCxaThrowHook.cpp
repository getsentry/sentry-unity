#include <dlfcn.h>
#include <typeinfo>
#include <exception>
#include <pthread.h>

extern "C"
{
    // Function type __cxa_throw
    typedef void(*cxa_throw_type)(void*, std::type_info*, void(*)(void*));
    // Function type __cxa_rethrow
    typedef void(*cxa_rethrow_type)(void);

    static pthread_mutex_t guard = PTHREAD_MUTEX_INITIALIZER;
    static cxa_throw_type orig_cxa_throw = NULL;
    static cxa_rethrow_type orig_cxa_rethrow = NULL;

    void __cxa_throw(void *thrown_exception, std::type_info *tinfo, void (*dest)(void *)) __attribute__((weak));

    void __cxa_throw(void *thrown_exception, std::type_info *tinfo, void (*dest)(void *))
    {
        pthread_mutex_lock(&guard);
        if (!orig_cxa_throw) // Try to load the sentry cocoa hook
            orig_cxa_throw = (cxa_throw_type)dlsym(RTLD_DEFAULT, "__sentry_cxa_throw");
        if (!orig_cxa_throw) // Try to load the default __cxa_throw method
            orig_cxa_throw = (cxa_throw_type)dlsym(RTLD_NEXT, "__cxa_throw");
        pthread_mutex_unlock(&guard);
        
        if (orig_cxa_throw)
            orig_cxa_throw(thrown_exception, tinfo, dest);
        else
            std::terminate();
        
        __builtin_unreachable();
    }

    void __cxa_rethrow() __attribute__((weak));

    void __cxa_rethrow()
    {
        pthread_mutex_lock(&guard);
        if (!orig_cxa_rethrow) // Try to load the sentry cocoa hook
            orig_cxa_rethrow = (cxa_rethrow_type)dlsym(RTLD_DEFAULT, "__sentry_cxa_rethrow");
        if (!orig_cxa_rethrow) // Try to load the default __cxa_rethrow method
            orig_cxa_rethrow = (cxa_rethrow_type)dlsym(RTLD_NEXT, "__cxa_rethrow");
        pthread_mutex_unlock(&guard);
        
        if (orig_cxa_rethrow)
            orig_cxa_rethrow();
        else
            std::terminate();
        
        __builtin_unreachable();
    }
}

