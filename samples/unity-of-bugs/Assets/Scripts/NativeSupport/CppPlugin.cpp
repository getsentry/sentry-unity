#include "il2cpp-api.h"
#include "il2cpp-config.h"
#include "os/Image.h"
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
        // throws std::length_error
        std::string("1").substr(2);
    } catch (const std::exception &e) {
        std::cout << e.what() << '\n';
        throw;
    }
}
}
