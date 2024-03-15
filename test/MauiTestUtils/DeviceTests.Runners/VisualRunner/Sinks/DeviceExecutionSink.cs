#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
    internal class DeviceExecutionSink : TestMessageSink

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		readonly SynchronizationContext _context;
		readonly ITestListener _listener;
		readonly Dictionary<ITestCase, TestCaseViewModel> _testCases;
After:
        private readonly SynchronizationContext _context;
        private readonly ITestListener _listener;
        private readonly Dictionary<ITestCase, TestCaseViewModel> _testCases;
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		readonly SynchronizationContext _context;
		readonly ITestListener _listener;
		readonly Dictionary<ITestCase, TestCaseViewModel> _testCases;
After:
        private readonly SynchronizationContext _context;
        private readonly ITestListener _listener;
        private readonly Dictionary<ITestCase, TestCaseViewModel> _testCases;
*/
    {
        private readonly SynchronizationContext _context;
        private readonly ITestListener _listener;
        private readonly Dictionary<ITestCase, TestCaseViewModel> _testCases;

        public DeviceExecutionSink(
            Dictionary<ITestCase, TestCaseViewModel> testCases,
            ITestListener listener,
            SynchronizationContext context)
        {
            _testCases = testCases ?? throw new ArgumentNullException(nameof(testCases));
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            Execution.TestFailedEvent += HandleTestFailed;
            Execution.TestPassedEvent += HandleTestPassed;
            Execution.TestSkippedEvent += HandleTestSkipped;

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
After:
        private void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
After:
        private void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
*/
        }

        private void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
        {
            MakeTestResultViewModel(args.Message, TestState.Failed);

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
After:
        private void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
After:
        private void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
*/
        }

        private void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
        {
            MakeTestResultViewModel(args.Message, TestState.Passed);

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
After:
        private void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
After:
        private void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
*/
        }

        private void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
        {
            MakeTestResultViewModel(args.Message, TestState.Skipped);

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
After:
        private async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
After:
        private async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
*/
        }

        private async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
        {
            var tcs = new TaskCompletionSource<TestResultViewModel>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_testCases.TryGetValue(testResult.TestCase, out TestCaseViewModel? testCase))
            {
                // no matching reference, search by Unique ID as a fallback
                testCase = _testCases.FirstOrDefault(kvp => kvp.Key.UniqueID?.Equals(testResult.TestCase.UniqueID, StringComparison.Ordinal) ?? false).Value;

                if (testCase == null)
                    return;
            }

            // Create the result VM on the UI thread as it updates properties
            _context.Post(_ =>
            {
                var result = new TestResultViewModel(testCase, testResult)
                {
                    Duration = TimeSpan.FromSeconds((double)testResult.ExecutionTime)
                };

                if (outcome == TestState.Failed)
                {
                    result.ErrorMessage = ExceptionUtility.CombineMessages((ITestFailed)testResult);
                    result.ErrorStackTrace = ExceptionUtility.CombineStackTraces((ITestFailed)testResult);
                }

                tcs.TrySetResult(result);
            }, null);

            var r = await tcs.Task;

            _listener.RecordResult(r); // bring it back to the threadpool thread
        }
    }
}
