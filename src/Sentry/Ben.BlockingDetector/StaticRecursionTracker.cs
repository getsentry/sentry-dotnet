namespace Sentry.Ben.BlockingDetector;

internal class StaticRecursionTracker : IRecursionTracker
{
    [ThreadStatic] private static int RecursionCount;

    public void Recurse() => RecursionCount++;
    public void Backtrack()
    {
        if (RecursionCount > 0)
        {
            RecursionCount--;
        }
    }
    public bool IsRecursive() => RecursionCount > 0;
    public bool IsFirstRecursion() => RecursionCount == 1;
}
