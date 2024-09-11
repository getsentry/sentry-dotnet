using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Microsoft.Maui.DeviceTests;

public static partial class AssertionExtensions
{
    static private readonly Random rnd = new Random();

    public static async Task<bool> Wait(Func<bool> exitCondition, int timeout = 1000)
    {
        while ((timeout -= 100) > 0)
        {
            if (!exitCondition.Invoke())
                await Task.Delay(rnd.Next(100, 200));
            else
                break;
        }

        return exitCondition.Invoke();
    }

    public static void AssertWithMessage(Action assertion, string message)
    {
        try
        {
            assertion();
        }
        catch (Exception e)
        {
            Assert.Fail($"Message: {message} Failure: {e}");
        }
    }

    public static void CloseEnough(double expected, double actual, double epsilon = 0.2, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
            message = " " + message;

        var diff = Math.Abs(expected - actual);
        Assert.True(diff <= epsilon, $"Expected: {expected}. Actual: {actual}. Diff: {diff} Epsilon: {epsilon}.{message}");
    }
}
