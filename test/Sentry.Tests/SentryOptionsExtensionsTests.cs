#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
#endif

#nullable enable

namespace Sentry.Tests;

public class SentryOptionsExtensionsTests
{
    private SentryOptions Sut { get; set; } = new();

    [Fact]
    public void DisableDuplicateEventDetection_RemovesDisableDuplicateEventDetection()
    {
        Sut.DisableDuplicateEventDetection();
        Assert.DoesNotContain(Sut.EventProcessors,
            p => p.GetType() == typeof(DuplicateEventDetectionEventProcessor));
    }

#if NETFRAMEWORK
    [Fact]
    public void DisableNetFxInstallationsEventProcessor_RemovesDisableNetFxInstallationsEventProcessorEventProcessor()
    {
        Sut.DisableNetFxInstallationsIntegration();
        Assert.DoesNotContain(Sut.EventProcessors,
            p => p.GetType() == typeof(NetFxInstallationsEventProcessor));
        Assert.DoesNotContain(Sut.Integrations,
            p => p.GetType() == typeof(NetFxInstallationsIntegration));
    }
#endif

    [Fact]
    public void DisableAppDomainUnhandledExceptionCapture_RemovesAppDomainUnhandledExceptionIntegration()
    {
        Sut.DisableAppDomainUnhandledExceptionCapture();
        Assert.DoesNotContain(Sut.Integrations,
            p => p is AppDomainUnhandledExceptionIntegration);
    }

    [Fact]
    public void DisableTaskUnobservedTaskExceptionCapture_UnobservedTaskExceptionIntegration()
    {
        Sut.DisableUnobservedTaskExceptionCapture();
        Assert.DoesNotContain(Sut.Integrations,
            p => p is UnobservedTaskExceptionIntegration);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void DisableSystemDiagnosticsMetricsIntegration_RemovesSystemDiagnosticsMetricsIntegration()
    {
        Sut.DisableSystemDiagnosticsMetricsIntegration();
        Assert.DoesNotContain(Sut.Integrations,
            p => p.GetType() == typeof(SystemDiagnosticsMetricsIntegration));
    }
#endif

    [Fact]
    public void AddIntegration_StoredInOptions()
    {
        var expected = Substitute.For<ISdkIntegration>();
        Sut.AddIntegration(expected);
        Assert.Contains(Sut.Integrations, actual => actual == expected);
    }

    [Fact]
    public void AddInAppExclude_StoredInOptions()
    {
        const string expected = "test";
        Sut.AddInAppExclude(expected);
        Assert.Contains(Sut.InAppExclude!, actual => actual == expected);
    }

    [Fact]
    public void AddInAppInclude_StoredInOptions()
    {
        const string expected = "test";
        Sut.AddInAppInclude(expected);
        Assert.Contains(Sut.InAppInclude!, actual => actual == expected);
    }

    [Fact]
    public void AddExceptionProcessor_StoredInOptions()
    {
        var expected = Substitute.For<ISentryEventExceptionProcessor>();
        Sut.AddExceptionProcessor(expected);
        Assert.Contains(Sut.ExceptionProcessors, actual => actual.Lazy.Value == expected);
    }

    [Fact]
    public void AddExceptionProcessors_StoredInOptions()
    {
        var first = Substitute.For<ISentryEventExceptionProcessor>();
        var second = Substitute.For<ISentryEventExceptionProcessor>();
        Sut.AddExceptionProcessors(new[] { first, second });
        Assert.Contains(Sut.ExceptionProcessors, actual => actual.Lazy.Value == first);
        Assert.Contains(Sut.ExceptionProcessors, actual => actual.Lazy.Value == second);
    }

    [Fact]
    public void AddExceptionProcessorProvider_StoredInOptions()
    {
        var first = Substitute.For<ISentryEventExceptionProcessor>();
        var second = Substitute.For<ISentryEventExceptionProcessor>();
        Sut.AddExceptionProcessorProvider(() => new[] { first, second });
        Assert.Contains(Sut.GetAllExceptionProcessors(), actual => actual == first);
        Assert.Contains(Sut.GetAllExceptionProcessors(), actual => actual == second);
    }

    [Fact]
    public void AddExceptionProcessor_DoesNotExcludeMainProcessor()
    {
        Sut.AddExceptionProcessor(Substitute.For<ISentryEventExceptionProcessor>());
        Assert.Contains(Sut.ExceptionProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void AddExceptionProcessors_DoesNotExcludeMainProcessor()
    {
        Sut.AddExceptionProcessors(new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        Assert.Contains(Sut.ExceptionProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void AddExceptionProcessorProvider_DoesNotExcludeMainProcessor()
    {
        Sut.AddExceptionProcessorProvider(() => new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        Assert.Contains(Sut.ExceptionProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void GetAllExceptionProcessors_ReturnsMainSentryEventProcessor()
    {
        Assert.Contains(Sut.GetAllExceptionProcessors(), actual => actual.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void GetAllExceptionProcessors_FirstReturned_MainExceptionProcessor()
    {
        Sut.AddExceptionProcessorProvider(() => new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        Sut.AddExceptionProcessors(new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        Sut.AddExceptionProcessor(Substitute.For<ISentryEventExceptionProcessor>());

        _ = Assert.IsType<MainExceptionProcessor>(Sut.GetAllExceptionProcessors().First());
    }

    [Fact]
    public void AddEventProcessor_StoredInOptions()
    {
        var expected = Substitute.For<ISentryEventProcessor>();
        Sut.AddEventProcessor(expected);
        Assert.Contains(Sut.EventProcessors, actual => actual.Lazy.Value == expected);
    }

    [Fact]
    public void AddEventProcessors_StoredInOptions()
    {
        var first = Substitute.For<ISentryEventProcessor>();
        var second = Substitute.For<ISentryEventProcessor>();
        Sut.AddEventProcessors(new[] { first, second });
        Assert.Contains(Sut.EventProcessors, actual => actual.Lazy.Value == first);
        Assert.Contains(Sut.EventProcessors, actual => actual.Lazy.Value == second);
    }

    [Fact]
    public void AddEventProcessorProvider_StoredInOptions()
    {
        var first = Substitute.For<ISentryEventProcessor>();
        var second = Substitute.For<ISentryEventProcessor>();
        Sut.AddEventProcessorProvider(() => new[] { first, second });
        Assert.Contains(Sut.GetAllEventProcessors(), actual => actual == first);
        Assert.Contains(Sut.GetAllEventProcessors(), actual => actual == second);
    }

    [Fact]
    public void AddTransactionProcessor_StoredInOptions()
    {
        var expected = Substitute.For<ISentryTransactionProcessor>();
        Sut.AddTransactionProcessor(expected);
        Assert.Contains(Sut.TransactionProcessors!, actual => actual == expected);
    }

    [Fact]
    public void AddTransactionProcessors_StoredInOptions()
    {
        var first = Substitute.For<ISentryTransactionProcessor>();
        var second = Substitute.For<ISentryTransactionProcessor>();
        Sut.AddTransactionProcessors(new[] { first, second });
        Assert.Contains(Sut.TransactionProcessors!, actual => actual == first);
        Assert.Contains(Sut.TransactionProcessors!, actual => actual == second);
    }

    [Fact]
    public void AddTransactionProcessorProvider_StoredInOptions()
    {
        var first = Substitute.For<ISentryTransactionProcessor>();
        var second = Substitute.For<ISentryTransactionProcessor>();
        Sut.AddTransactionProcessorProvider(() => new[] { first, second });
        Assert.Contains(Sut.GetAllTransactionProcessors(), actual => actual == first);
        Assert.Contains(Sut.GetAllTransactionProcessors(), actual => actual == second);
    }

    [Fact]
    public void AddEventProcessor_DoesNotExcludeMainProcessor()
    {
        Sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());
        Assert.Contains(Sut.EventProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void AddEventProcessors_DoesNotExcludeMainProcessor()
    {
        Sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
        Assert.Contains(Sut.EventProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void AddEventProcessorProvider_DoesNotExcludeMainProcessor()
    {
        Sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
        Assert.Contains(Sut.EventProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void GetAllEventProcessors_ReturnsMainSentryEventProcessor()
    {
        Assert.Contains(Sut.GetAllEventProcessors(), actual => actual.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void GetAllEventProcessors_AddingMore_SecondReturned_MainSentryEventProcessor()
    {
        Sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
        Sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
        Sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());

        _ = Assert.IsType<MainSentryEventProcessor>(Sut.GetAllEventProcessors().Skip(1).First());
    }

    [Fact]
    public void GetAllEventProcessors_NoAdding_SecondReturned_MainSentryEventProcessor()
    {
        _ = Assert.IsType<MainSentryEventProcessor>(Sut.GetAllEventProcessors().Skip(1).First());
    }

    [Fact]
    public void UseStackTraceFactory_ReplacesStackTraceFactory()
    {
        var expected = Substitute.For<ISentryStackTraceFactory>();
        _ = Sut.UseStackTraceFactory(expected);

        Assert.Same(expected, Sut.SentryStackTraceFactory);
    }

    [Fact]
    public void UseStackTraceFactory_ReplacesStackTraceFactory_InCurrentProcessors()
    {
        var eventProcessor = Sut.GetAllEventProcessors().OfType<MainSentryEventProcessor>().Single();
        var exceptionProcessor = Sut.GetAllExceptionProcessors().OfType<MainExceptionProcessor>().Single();

        var expected = Substitute.For<ISentryStackTraceFactory>();
        _ = Sut.UseStackTraceFactory(expected);

        Assert.Same(expected, eventProcessor.SentryStackTraceFactoryAccessor());
        Assert.Same(expected, exceptionProcessor.SentryStackTraceFactoryAccessor());
    }

    [Fact]
    public void UseStackTraceFactory_NotNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => Sut.UseStackTraceFactory(null!));
    }

    [Fact]
    public void GetAllEventProcessors_AddingMore_FirstReturned_DuplicateDetectionProcessor()
    {
        Sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
        Sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
        Sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());

        _ = Assert.IsType<DuplicateEventDetectionEventProcessor>(Sut.GetAllEventProcessors().First());
    }

    [Fact]
    public void GetAllEventProcessors_NoAdding_FirstReturned_DuplicateDetectionProcessor()
    {
        _ = Assert.IsType<DuplicateEventDetectionEventProcessor>(Sut.GetAllEventProcessors().First());
    }

    [Fact]
    public void Integrations_Includes_AppDomainUnhandledExceptionIntegration()
    {
        Assert.Contains(Sut.Integrations, i => i.GetType() == typeof(AppDomainUnhandledExceptionIntegration));
    }

    [Fact]
    public void Integrations_Includes_AppDomainProcessExitIntegration()
    {
        Assert.Contains(Sut.Integrations, i => i.GetType() == typeof(AppDomainProcessExitIntegration));
    }

    [Fact]
    public void Integrations_Includes_TaskUnobservedTaskExceptionIntegration()
    {
        Assert.Contains(Sut.Integrations, i => i.GetType() == typeof(UnobservedTaskExceptionIntegration));
    }

    [Theory]
    [InlineData("Microsoft")]
    [InlineData("System")]
    [InlineData("FSharp")]
    [InlineData("Giraffe")]
    [InlineData("Newtonsoft.Json")]
    public void Integrations_Includes_MajorSystemPrefixes(string expected)
    {
        Assert.Contains(Sut.InAppExclude!, e => e == expected);
    }
}
