#include <string>
#include <iostream>
#include <exception>

extern "C" {
	void crash_in_cpp()
	{
		int a = 5, b = 0, c = 0;
		int *p = 0;
		p = &a;
		b = *p;
		p = 0;
		c = *p;
	}
}

extern "C" {
	void throw_in_cpp()
	{
	    try {
            std::string("abc").substr(10); // throws std::length_error
        } catch(const std::exception& e) {
            std::cout << e.what() << '\n';
            throw;   // rethrows the exception object of type std::length_error
        }
	}
}
