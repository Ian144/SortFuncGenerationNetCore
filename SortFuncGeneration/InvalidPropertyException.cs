using System;


namespace SortFuncGeneration;

#pragma warning disable CA1032
public class InvalidPropertyException : Exception
#pragma warning restore CA1032
{
    public InvalidPropertyException(string message) : base(message)
    {
    }

    public InvalidPropertyException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // public InvalidPropertyException() : base("An invalid property was specified")
    // {
    // }
}
