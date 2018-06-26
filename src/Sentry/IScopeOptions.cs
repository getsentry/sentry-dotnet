namespace Sentry
{
    public interface IScopeOptions
    {
        int MaxBreadcrumbs { get; }
        bool Locked { get; set; }
    }
}
