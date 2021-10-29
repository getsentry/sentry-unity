#ifdef _WIN32
__declspec(noinline)
#else
__attribute__((noinline))
#endif

    void crash_in_c()
{
    char *ptr = 0;
    *ptr += 1;
}
