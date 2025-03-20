namespace Sentry.Ben.BlockingDetector;

internal interface IRecursionTracker
{
    public void Recurse();
    public void Backtrack();
    public bool IsFirstRecursion();
}
