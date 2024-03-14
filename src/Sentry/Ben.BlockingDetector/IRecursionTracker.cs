namespace Sentry.Ben.BlockingDetector;

internal interface IRecursionTracker
{
    void Recurse();
    void Backtrack();
    bool IsFirstRecursion();
}
