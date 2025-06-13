using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentry.Maui.Internal;

namespace Sentry.Maui.CommunityToolkit.Mvvm.Tests;

public class MauiCommunityToolkitMvvmEventsBinderTests
{
    private class Fixture
    {
        public IHub Hub { get; }

        public MauiEventsBinder Binder { get; }

        public Scope Scope { get; } = new();

        public SentryMauiOptions Options { get; } = new();

        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            Hub.SubstituteConfigureScope(Scope);

            Options.Debug = true;
            var logger = Substitute.For<IDiagnosticLogger>();
            logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Options.DiagnosticLogger = logger;
            var options = Microsoft.Extensions.Options.Options.Create(Options);
            Binder = new MauiEventsBinder(
                Hub,
                options,
                [
                    new MauiCommunityToolkitMvvmEventsBinder(Hub)
                ]
            );
        }
    }

    private readonly Fixture _fixture = new();

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsyncRelayCommand_AddsTransactionsOrSpans(bool isActiveTransaction)
    {
        // TODO: See if we can resolve this and reinstate the test
        Skip.If(TestEnvironment.IsGitHubActions, "Flaky on CI");

        // Arrange
        var transaction = Substitute.For<ITransactionTracer>();
        var span = Substitute.For<ISpan>();
        if (isActiveTransaction)
        {
            transaction.When(t => transaction.StartChild(default).Returns(span));
            _fixture.Scope.Transaction = transaction;
        }
        else
        {
            _fixture.Hub.StartTransaction(default, default).Returns(transaction);
        }

        var vm = new TestCtMvvmViewModel();
        var button = new Button
        {
            Command = vm.TestCommand
        };
        var binder = new MauiCommunityToolkitMvvmEventsBinder(_fixture.Hub);
        binder.Bind(button, _ => { });

        // Act
        button.Command.Execute(null!);

        // Wait for the command to finish
        await vm.TestCommand.ExecutionTask!;

        // Assert
        if (isActiveTransaction)
        {
            transaction!.Received(1).StartChild(Arg.Any<string>());
        }
        else
        {
            _fixture.Hub.Received(1).StartTransaction(Arg.Any<TransactionContext>(), Arg.Any<Dictionary<string, object>>());
        }
    }
}

public partial class TestCtMvvmViewModel() : ObservableObject
{
    [RelayCommand]
    private async Task Test()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));
    }
}
