using System;

namespace PhotoBank.Services
{
    public interface IOrderDependant
    {
        Type[] Dependencies { get; }
    }
}