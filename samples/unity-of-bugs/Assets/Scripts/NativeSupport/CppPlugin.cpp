#include <exception>
#include <iostream>
#include <string>

extern "C" {
void crash_in_cpp()
{
    char *ptr = 0;
    *ptr += 1;
}
}

extern "C" {
void throw_cpp()
{
    try {
        throw std::runtime_error("test");
    } catch (const std::exception &e) {
        std::cout << e.what() << '\n';
        throw;
    }
}
}
