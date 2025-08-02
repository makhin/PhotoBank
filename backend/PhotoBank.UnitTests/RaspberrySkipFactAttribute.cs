using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace PhotoBank.UnitTests;

/// <summary>
/// NUnit test attribute that skips execution when running on Raspberry Pi (ARM) architecture.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RaspberrySkipFactAttribute : TestAttribute, IApplyToTest
{
    public new void ApplyToTest(Test test)
    {
        if (RuntimeInformation.OSArchitecture == Architecture.Arm ||
            RuntimeInformation.OSArchitecture == Architecture.Arm64)
        {
            test.RunState = RunState.Ignored;
            test.Properties.Set(PropertyNames.SkipReason, "Skipped on Raspberry Pi");
        }
    }
}
