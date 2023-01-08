using System;


public class CircleDependencyException : Exception
{
    //: base($"There are circle dependency reference between type {mainType} & {argType}") 
    public CircleDependencyException(Type mainType, Type argType) : base($"There are circle dependency reference between type {mainType} & {argType}")
    {

    }
}
