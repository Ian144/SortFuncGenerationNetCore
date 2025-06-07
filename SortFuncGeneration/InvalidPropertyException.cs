using System;


namespace SortFuncGeneration;

public class InvalidPropertyException(string message) : Exception(message)
{
    public InvalidPropertyException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public InvalidPropertyException() : this()
    {
    }
}