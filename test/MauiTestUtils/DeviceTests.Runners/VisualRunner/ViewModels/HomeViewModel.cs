#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
    public class HomeViewModel : ViewModelBase

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
    Before:
            readonly ITestNavigation _navigation;
            readonly ITestRunner _runner;
    After:
            private readonly ITestNavigation _navigation;
            private readonly ITestRunner _runner;
    */

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
    Before:
            readonly ITestNavigation _navigation;
            readonly ITestRunner _runner;
    After:
            private readonly ITestNavigation _navigation;
            private readonly ITestRunner _runner;
    */
    {
        private readonly ITestNavigation _navigation;
        private readonly ITestRunner _runner;
        private string _diagnosticMessages = string.Empty;
        private bool _loaded;

        /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
        Added:
                private bool _isBusy;
        */

        /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
        Added:
                private bool _isBusy;
        */
        private bool _isBusy;

        internal HomeViewModel(ITestNavigation navigation, ITestRunner runner)
        {
            _navigation = navigation;
            _runner = runner;

            _runner.OnDiagnosticMessage += RunnerOnOnDiagnosticMessage;

            TestAssemblies = new ObservableCollection<TestAssemblyViewModel>();

            CreditsCommand = new Command(CreditsExecute);
            RunEverythingCommand = new Command(RunEverythingExecute, () => !_isBusy);
            NavigateToTestAssemblyCommand = new Command<TestAssemblyViewModel?>(NavigateToTestAssemblyExecute);
        }

        public ObservableCollection<TestAssemblyViewModel> TestAssemblies { get; private set; }

        public Command CreditsCommand { get; }

        public Command RunEverythingCommand { get; }

        public Command NavigateToTestAssemblyCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set => Set(ref _isBusy, value, RunEverythingCommand.ChangeCanExecute);
        }

        public string DiagnosticMessages
        {
            get => _diagnosticMessages;
            private set => Set(ref _diagnosticMessages, value);
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            await StartAssemblyScanAsync();
        }

        public async Task StartAssemblyScanAsync()
        {
            if (_loaded)
                return;

            IsBusy = true;

            try
            {
                var allTests = await _runner.DiscoverAsync();

                TestAssemblies = new ObservableCollection<TestAssemblyViewModel>(allTests);
                RaisePropertyChanged(nameof(TestAssemblies));
            }
            finally
            {
                IsBusy = false;
                _loaded = true;

                /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
                Before:
                        async void CreditsExecute()
                        {
                            await _navigation.NavigateTo(PageType.Credits);
                        }

                        async void RunEverythingExecute()
                        {
                            try
                            {
                                IsBusy = true;

                                if (!string.IsNullOrWhiteSpace(DiagnosticMessages))
                                    DiagnosticMessages += $"----------{Environment.NewLine}";

                                await _runner.RunAsync(TestAssemblies.Select(t => t.RunInfo).ToList(), "Run Everything");
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                        }

                        async void NavigateToTestAssemblyExecute(TestAssemblyViewModel? vm)
                After:
                        private async void CreditsExecute()
                        {
                            await _navigation.NavigateTo(PageType.Credits);
                        }

                        private async void RunEverythingExecute()
                        {
                            try
                            {
                                IsBusy = true;

                                if (!string.IsNullOrWhiteSpace(DiagnosticMessages))
                                    DiagnosticMessages += $"----------{Environment.NewLine}";

                                await _runner.RunAsync(TestAssemblies.Select(t => t.RunInfo).ToList(), "Run Everything");
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                        }

                        private async void NavigateToTestAssemblyExecute(TestAssemblyViewModel? vm)
                */

                /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
                Before:
                        async void CreditsExecute()
                        {
                            await _navigation.NavigateTo(PageType.Credits);
                        }

                        async void RunEverythingExecute()
                        {
                            try
                            {
                                IsBusy = true;

                                if (!string.IsNullOrWhiteSpace(DiagnosticMessages))
                                    DiagnosticMessages += $"----------{Environment.NewLine}";

                                await _runner.RunAsync(TestAssemblies.Select(t => t.RunInfo).ToList(), "Run Everything");
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                        }

                        async void NavigateToTestAssemblyExecute(TestAssemblyViewModel? vm)
                After:
                        private async void CreditsExecute()
                        {
                            await _navigation.NavigateTo(PageType.Credits);
                        }

                        private async void RunEverythingExecute()
                        {
                            try
                            {
                                IsBusy = true;

                                if (!string.IsNullOrWhiteSpace(DiagnosticMessages))
                                    DiagnosticMessages += $"----------{Environment.NewLine}";

                                await _runner.RunAsync(TestAssemblies.Select(t => t.RunInfo).ToList(), "Run Everything");
                            }
                            finally
                            {
                                IsBusy = false;
                            }
                        }

                        private async void NavigateToTestAssemblyExecute(TestAssemblyViewModel? vm)
                */
            }
        }

        private async void CreditsExecute()
        {
            await _navigation.NavigateTo(PageType.Credits);
        }

        private async void RunEverythingExecute()
        {
            try
            {
                IsBusy = true;

                if (!string.IsNullOrWhiteSpace(DiagnosticMessages))
                    DiagnosticMessages += $"----------{Environment.NewLine}";

                await _runner.RunAsync(TestAssemblies.Select(t => t.RunInfo).ToList(), "Run Everything");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void NavigateToTestAssemblyExecute(TestAssemblyViewModel? vm)
        {
            if (vm == null)
                return;

            await _navigation.NavigateTo(PageType.AssemblyTestList, vm);

            /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
            Before:
                    void RunnerOnOnDiagnosticMessage(string s)
            After:
                    private void RunnerOnOnDiagnosticMessage(string s)
            */

            /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
            Before:
                    void RunnerOnOnDiagnosticMessage(string s)
            After:
                    private void RunnerOnOnDiagnosticMessage(string s)
            */
        }

        private void RunnerOnOnDiagnosticMessage(string s)
        {
            DiagnosticMessages += $"{s}{Environment.NewLine}{Environment.NewLine}";
        }
    }
}
