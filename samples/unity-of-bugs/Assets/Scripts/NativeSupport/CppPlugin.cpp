#include <exception>
#include <iostream>
#include <string>
#include "il2cpp-api.h"
#include "il2cpp-config.h"
#include "os/Image.h"

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

void get_current_thread_native_stack_trace(uintptr_t** addresses, int* numFrames, char* imageUUID)
{
    *numFrames = il2cpp_current_thread_get_stack_depth();

    *addresses = static_cast<uintptr_t*>(il2cpp_alloc((*numFrames) * sizeof(uintptr_t)));

    for (int32_t i = 0; i < *numFrames; ++i) {
        Il2CppStackFrameInfo frame;
        if (il2cpp_current_thread_get_frame_at(i, &frame)) {
            (*addresses)[i] = frame.raw_ip;
        }
    }  

    imageUUID = il2cpp::os::Image::GetImageUUID();
}


}
