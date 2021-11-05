using System;

#pragma warning disable RCS1194 // Implement exception constructors.
class Error : Exception
#pragma warning restore RCS1194 // Implement exception constructors.
{
    public Error(string message) : base(message)
    {
    }
}
