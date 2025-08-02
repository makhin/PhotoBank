using System;

namespace PhotoBank.Services
{
    public interface IOrderDependent
    {
        Type[] Dependencies { get; }
    }
}