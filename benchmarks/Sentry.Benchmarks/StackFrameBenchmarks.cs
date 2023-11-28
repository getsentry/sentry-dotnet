using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;

namespace Sentry.Benchmarks;

public class StackFrameBenchmarks
{
    private SentryOptions _options = new();
    private SentryStackFrame[] data;

    [Params(1000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        data = new SentryStackFrame[N];
        var rand = new Random(42);
        for (var i = 0; i < N; i++)
        {
            var proto = _framePrototypes[rand.NextInt64(0, _framePrototypes.Length)];
            data[i] = new()
            {
                Function = proto.Function,
                Module = rand.NextSingle() < 0.5 ? null : proto.Module
            };
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        for (var i = 0; i < N; i++)
        {
            data[i].InApp = null;
        }
    }


    [Benchmark]
    public void ConfigureAppFrame()
    {
        for (var i = 0; i < N; i++)
        {
            data[i].ConfigureAppFrame(_options);
        }
    }

    // These are real frames captured by a profiler session on Sentry.Samples.Console.Customized.
    private SentryStackFrame[] _framePrototypes = new SentryStackFrame[] {
        new SentryStackFrame() {
          Function ="System.Threading.Monitor.Wait(class System.Object,int32)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ManualResetEventSlim.Wait(int32,value class System.Threading.CancellationToken)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.SpinThenBlockingWait(int32,value class System.Threading.CancellationToken)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.InternalWaitCore(int32,value class System.Threading.CancellationToken)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(class System.Threading.Tasks.Task)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.TaskAwaiter.GetResult()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Threading.LowLevelLifoSemaphore.WaitForSignal(int32)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.LowLevelLifoSemaphore.Wait(int32,bool)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Thread.StartCallback()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.WaitHandle.WaitOneNoCheck(int32)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.WaitHandle.WaitOne(int32)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.PortableThreadPool+GateThread.GateThreadStart()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.IO.Pipes.PipeStream.ReadCore(value class System.Span`1<unsigned int8>)",
          Module ="System.IO.Pipes.il"
        },
        new SentryStackFrame() {
          Function ="System.IO.Pipes.PipeStream.Read(unsigned int8[],int32,int32)",
          Module ="System.IO.Pipes.il"
        },
        new SentryStackFrame() {
          Function ="System.IO.BinaryReader.ReadBytes(int32)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Microsoft.Diagnostics.NETCore.Client.IpcHeader.Parse(class System.IO.BinaryReader) {QuickJitted}",
          Module ="Microsoft.Diagnostics.NETCore.Client"
        },
        new SentryStackFrame() {
          Function ="Microsoft.Diagnostics.NETCore.Client.IpcMessage.Parse(class System.IO.Stream) {QuickJitted}",
          Module ="Microsoft.Diagnostics.NETCore.Client"
        },
        new SentryStackFrame() {
          Function ="Microsoft.Diagnostics.NETCore.Client.IpcClient.Read(class System.IO.Stream) {QuickJitted}",
          Module ="Microsoft.Diagnostics.NETCore.Client"
        },
        new SentryStackFrame() {
          Function ="Microsoft.Diagnostics.NETCore.Client.IpcClient.SendMessageGetContinuation(class Microsoft.Diagnostics.NETCore.Client.IpcEndpoint,class Microsoft.Diagnostics.NETCore.Client.IpcMessage) {QuickJitted}",
          Module ="Microsoft.Diagnostics.NETCore.Client"
        },
        new SentryStackFrame() {
          Function ="Microsoft.Diagnostics.NETCore.Client.EventPipeSession.Start(class Microsoft.Diagnostics.NETCore.Client.IpcEndpoint,class System.Collections.Generic.IEnumerable`1<class Microsoft.Diagnostics.NETCore.Client.EventPipeProvider>,bool,int32) {QuickJitted}",
          Module ="Microsoft.Diagnostics.NETCore.Client"
        },
        new SentryStackFrame() {
          Function ="Microsoft.Diagnostics.NETCore.Client.DiagnosticsClient.StartEventPipeSession(class System.Collections.Generic.IEnumerable`1<class Microsoft.Diagnostics.NETCore.Client.EventPipeProvider>,bool,int32) {QuickJitted}",
          Module ="Microsoft.Diagnostics.NETCore.Client"
        },
        new SentryStackFrame() {
          Function ="ProfilerSession..ctor(class System.Object) {QuickJitted}",
          Module ="Sentry.Extensions.Profiling"
        },
        new SentryStackFrame() {
          Function ="SamplingTransactionProfiler.Start(class Sentry.ITransactionTracer) {QuickJitted}",
          Module ="Sentry.Extensions.Profiling"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Hub.StartTransaction(class Sentry.ITransactionContext,class System.Collections.Generic.IReadOnlyDictionary`2<class System.String,class System.Object>,class Sentry.DynamicSamplingContext) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Hub.StartTransaction(class Sentry.ITransactionContext,class System.Collections.Generic.IReadOnlyDictionary`2<class System.String,class System.Object>) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="Program+<Main>d__2.MoveNext() {Optimized}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,Program+<Main>d__2].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop(class System.Threading.Thread,class System.Threading.ExecutionContext,class System.Threading.ContextCallback,class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,Program+<Main>d__2].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,Program+<Main>d__2].ExecuteFromThreadPool(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ThreadPoolWorkQueue.Dispatch()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.GzipBufferedRequestBodyHandler+<SendAsync>d__3.MoveNext() {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.GzipBufferedRequestBodyHandler.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.DelegatingHandler.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.RetryAfterHandler.<>n__0(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.RetryAfterHandler+<SendAsync>d__8.MoveNext() {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.RetryAfterHandler.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpMessageInvoker.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpClient+<<SendAsync>g__Core|83_0>d.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpClient.<SendAsync>g__Core|83_0(class System.Net.Http.HttpRequestMessage,value class System.Net.Http.HttpCompletionOption,class System.Threading.CancellationTokenSource,bool,class System.Threading.CancellationTokenSource,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpClient.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Net.Http.HttpCompletionOption,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpClient.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.HttpTransport+<SendEnvelopeAsync>d__3.MoveNext() {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.HttpTransport.SendEnvelopeAsync(class Sentry.Protocol.Envelopes.Envelope,value class System.Threading.CancellationToken) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.BackgroundWorker+<DoWorkAsync>d__20.MoveNext() {Optimized}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,Sentry.Internal.BackgroundWorker+<DoWorkAsync>d__20].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ExecutionContext.RunInternal(class System.Threading.ExecutionContext,class System.Threading.ContextCallback,class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,Sentry.Internal.BackgroundWorker+<DoWorkAsync>d__20].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,Sentry.Internal.BackgroundWorker+<DoWorkAsync>d__20].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.AwaitTaskContinuation.RunOrScheduleAction(class System.Runtime.CompilerServices.IAsyncStateMachineBox,bool)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.RunContinuations(class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.FinishContinuations()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.Boolean].TrySetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.Boolean].SetExistingTaskResult(class System.Threading.Tasks.Task`1<!0>,!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.SemaphoreSlim+<WaitUntilCountOrTimeoutAsync>d__31.MoveNext()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Boolean,System.Threading.SemaphoreSlim+<WaitUntilCountOrTimeoutAsync>d__31].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Boolean,System.Threading.SemaphoreSlim+<WaitUntilCountOrTimeoutAsync>d__31].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Boolean,System.Threading.SemaphoreSlim+<WaitUntilCountOrTimeoutAsync>d__31].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.AwaitTaskContinuation.RunOrScheduleAction(class System.Action,bool)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.RunContinuations(class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task+CancellationPromise`1[System.Boolean].System.Threading.Tasks.ITaskCompletionAction.Invoke(class System.Threading.Tasks.Task)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.CompletionActionInvoker.System.Threading.IThreadPoolWorkItem.Execute()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ThreadPoolWorkQueue.Dispatch()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.FindPrimeNumber(int32) {Optimized}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program+<Main>d__2.MoveNext() {Optimized}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.WinInetProxyHelper..ctor()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpWindowsProxy.TryCreate(class System.Net.IWebProxy&)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.SystemProxyInfo.ConstructSystemProxy()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Lazy`1[System.__Canon].ViaFactory(value class System.Threading.LazyThreadSafetyMode)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Lazy`1[System.__Canon].ExecutionAndPublication(class System.LazyHelper,bool)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Lazy`1[System.__Canon].CreateValue()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Lazy`1[System.__Canon].get_Value()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpClient+<>c.<get_DefaultProxy>b__15_0()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.LazyInitializer.EnsureInitializedCore(!!0&,class System.Func`1<!!0>)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.LazyInitializer.EnsureInitialized(!!0&,class System.Func`1<!!0>)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpClient.get_DefaultProxy()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPoolManager..ctor(class System.Net.Http.HttpConnectionSettings)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.SocketsHttpHandler.SetupHandlerChain()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.SocketsHttpHandler.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.DelegatingHandler.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="Sentry.Internal.Http.GzipBufferedRequestBodyHandler.<>n__0(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken) {QuickJitted}",
          Module ="Sentry"
        },
        new SentryStackFrame() {
          Function ="System.IO.Stream+<>c.<BeginReadInternal>b__40_0(class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.Int32].InnerInvoke()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task+<>c.<.cctor>b__272_0(class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.ExecuteWithThreadLocal(class System.Threading.Tasks.Task&,class System.Threading.Thread)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.ExecuteEntryUnsafe(class System.Threading.Thread)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task.ExecuteFromThreadPool(class System.Threading.Thread)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Program+<>c.<Main>b__2_9() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.Int64].InnerInvoke()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.ValueTuple`2[System.__Canon,System.__Canon]].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(class System.String,int32,class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<ConnectAsync>d__97.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.ValueTuple`3[System.__Canon,System.__Canon,System.__Canon]].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool.ConnectAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<CreateHttp11ConnectionAsync>d__99.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool.CreateHttp11ConnectionAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<AddHttp11ConnectionAsync>d__74.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool.AddHttp11ConnectionAsync(class System.Net.Http.HttpRequestMessage)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<>c__DisplayClass75_0.<CheckForHttp11ConnectionInjection>b__0()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.__Canon].InnerInvoke()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon].AwaitUnsafeOnCompleted(!!0&,!!1&,class System.Threading.Tasks.Task`1<!0>&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].AwaitUnsafeOnCompleted(!!0&,!!1&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<GetHttp11ConnectionAsync>d__76.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool.GetHttp11ConnectionAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<SendWithVersionDetectionAndRetryAsync>d__84.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool.SendWithVersionDetectionAndRetryAsync(class System.Net.Http.HttpRequestMessage,bool,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPoolManager.SendAsyncCore(class System.Net.Http.HttpRequestMessage,class System.Uri,bool,bool,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPoolManager.SendAsync(class System.Net.Http.HttpRequestMessage,bool,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionHandler.SendAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpMessageHandlerStage.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.DiagnosticsHandler.SendAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.RedirectHandler+<SendAsync>d__4.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.RedirectHandler.SendAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.DecompressionHandler+<SendAsync>d__16.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.DecompressionHandler.SendAsync(class System.Net.Http.HttpRequestMessage,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.SocketsHttpHandler.SendAsync(class System.Net.Http.HttpRequestMessage,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SecureChannel.AcquireClientCredentials(unsigned int8[]&)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SecureChannel.GenerateToken(value class System.ReadOnlySpan`1<unsigned int8>,unsigned int8[]&)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SecureChannel.NextMessage(value class System.ReadOnlySpan`1<unsigned int8>)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream+<ForceAuthenticationAsync>d__175`1[System.Net.Security.AsyncReadWriteAdapter].MoveNext()",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream.ForceAuthenticationAsync(!!0,bool,unsigned int8[],bool)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream.ProcessAuthenticationAsync(bool,bool,value class System.Threading.CancellationToken)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream.AuthenticateAsClientAsync(class System.Net.Security.SslClientAuthenticationOptions,value class System.Threading.CancellationToken)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.ConnectHelper+<EstablishSslConnectionAsync>d__2.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.ConnectHelper.EstablishSslConnectionAsync(class System.Net.Security.SslClientAuthenticationOptions,class System.Net.Http.HttpRequestMessage,bool,class System.IO.Stream,value class System.Threading.CancellationToken)",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<ConnectAsync>d__97.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.ValueTuple`3[System.__Canon,System.__Canon,System.__Canon],System.Net.Http.HttpConnectionPool+<ConnectAsync>d__97].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.ValueTuple`3[System.__Canon,System.__Canon,System.__Canon],System.Net.Http.HttpConnectionPool+<ConnectAsync>d__97].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.ValueTuple`3[System.__Canon,System.__Canon,System.__Canon],System.Net.Http.HttpConnectionPool+<ConnectAsync>d__97].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.ValueTuple`2[System.__Canon,System.__Canon]].TrySetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.ValueTuple`2[System.__Canon,System.__Canon]].SetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Http.HttpConnectionPool+<ConnectToTcpHostAsync>d__98.MoveNext()",
          Module ="System.Net.Http.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.ValueTuple`2[System.__Canon,System.__Canon],System.Net.Http.HttpConnectionPool+<ConnectToTcpHostAsync>d__98].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.ValueTuple`2[System.__Canon,System.__Canon],System.Net.Http.HttpConnectionPool+<ConnectToTcpHostAsync>d__98].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.ValueTuple`2[System.__Canon,System.__Canon],System.Net.Http.HttpConnectionPool+<ConnectToTcpHostAsync>d__98].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.Threading.Tasks.VoidTaskResult].TrySetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.Threading.Tasks.VoidTaskResult].SetExistingTaskResult(class System.Threading.Tasks.Task`1<!0>,!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.SetResult()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.Socket+<<ConnectAsync>g__WaitForConnectWithCancellation|277_0>d.MoveNext()",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Sockets.Socket+<<ConnectAsync>g__WaitForConnectWithCancellation|277_0>d].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Sockets.Socket+<<ConnectAsync>g__WaitForConnectWithCancellation|277_0>d].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Sockets.Socket+<<ConnectAsync>g__WaitForConnectWithCancellation|277_0>d].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ThreadPool+<>c.<.cctor>b__87_0(class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.Socket+AwaitableSocketAsyncEventArgs.InvokeContinuation(class System.Action`1<class System.Object>,class System.Object,bool,bool)",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.Socket+AwaitableSocketAsyncEventArgs.OnCompleted(class System.Net.Sockets.SocketAsyncEventArgs)",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.SocketAsyncEventArgs+<<DnsConnectAsync>g__Core|112_0>d.MoveNext()",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Sockets.SocketAsyncEventArgs+<<DnsConnectAsync>g__Core|112_0>d].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Sockets.SocketAsyncEventArgs+<<DnsConnectAsync>g__Core|112_0>d].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Sockets.SocketAsyncEventArgs+<<DnsConnectAsync>g__Core|112_0>d].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1[System.Boolean].SignalCompletion()",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1[System.Boolean].SetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.SocketAsyncEventArgs+MultiConnectSocketAsyncEventArgs.OnCompleted(class System.Net.Sockets.SocketAsyncEventArgs)",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.SocketAsyncEventArgs.OnCompletedInternal()",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.SocketAsyncEventArgs.ExecutionCallback(class System.Object)",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ExecutionContext.Run(class System.Threading.ExecutionContext,class System.Threading.ContextCallback,class System.Object)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.SocketAsyncEventArgs+<>c.<.cctor>b__179_0(unsigned int32,unsigned int32,value class System.Threading.NativeOverlapped*)",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.ThreadPoolBoundHandleOverlapped.CompletionCallback(unsigned int32,unsigned int32,value class System.Threading.NativeOverlapped*)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading._IOCompletionCallback.PerformIOCompletionCallback(unsigned int32,unsigned int32,value class System.Threading.NativeOverlapped*)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon].AwaitUnsafeOnCompleted(!!0&,!!1&,class System.Threading.Tasks.Task`1<!0>&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].AwaitUnsafeOnCompleted(!!0&,!!1&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream+<ReceiveBlobAsync>d__176`1[System.Net.Security.AsyncReadWriteAdapter].MoveNext()",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].Start(!!0&) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream.ReceiveBlobAsync(!!0)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream+<ForceAuthenticationAsync>d__175`1[System.Net.Security.AsyncReadWriteAdapter].MoveNext()",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Internal.Cryptography.Pal.ChainPal.BuildChain(bool,class Internal.Cryptography.ICertificatePal,class System.Security.Cryptography.X509Certificates.X509Certificate2Collection,class System.Security.Cryptography.OidCollection,class System.Security.Cryptography.OidCollection,value class System.Security.Cryptography.X509Certificates.X509RevocationMode,value class System.Security.Cryptography.X509Certificates.X509RevocationFlag,class System.Security.Cryptography.X509Certificates.X509Certificate2Collection,value class System.Security.Cryptography.X509Certificates.X509ChainTrustMode,value class System.DateTime,value class System.TimeSpan,bool)",
          Module ="System.Security.Cryptography.X509Certificates.il"
        },
        new SentryStackFrame() {
          Function ="System.Security.Cryptography.X509Certificates.X509Chain.Build(class System.Security.Cryptography.X509Certificates.X509Certificate2,bool)",
          Module ="System.Security.Cryptography.X509Certificates.il"
        },
        new SentryStackFrame() {
          Function ="System.Security.Cryptography.X509Certificates.X509Chain.Build(class System.Security.Cryptography.X509Certificates.X509Certificate2)",
          Module ="System.Security.Cryptography.X509Certificates.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.CertificateValidation.BuildChainAndVerifyProperties(class System.Security.Cryptography.X509Certificates.X509Chain,class System.Security.Cryptography.X509Certificates.X509Certificate2,bool,bool,class System.String)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SecureChannel.VerifyRemoteCertificate(class System.Net.Security.RemoteCertificateValidationCallback,class System.Net.Security.SslCertificateTrust,class System.Net.Security.ProtocolToken&,value class System.Net.Security.SslPolicyErrors&,value class System.Security.Cryptography.X509Certificates.X509ChainStatusFlags&)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream.CompleteHandshake(class System.Net.Security.ProtocolToken&,value class System.Net.Security.SslPolicyErrors&,value class System.Security.Cryptography.X509Certificates.X509ChainStatusFlags&)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream.CompleteHandshake(class System.Net.Security.SslAuthenticationOptions)",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream+<ForceAuthenticationAsync>d__175`1[System.Net.Security.AsyncReadWriteAdapter].MoveNext()",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Security.SslStream+<ForceAuthenticationAsync>d__175`1[System.Net.Security.AsyncReadWriteAdapter]].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Security.SslStream+<ForceAuthenticationAsync>d__175`1[System.Net.Security.AsyncReadWriteAdapter]].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Threading.Tasks.VoidTaskResult,System.Net.Security.SslStream+<ForceAuthenticationAsync>d__175`1[System.Net.Security.AsyncReadWriteAdapter]].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.__Canon].TrySetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon].SetExistingTaskResult(class System.Threading.Tasks.Task`1<!0>,!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.__Canon].SetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream+<ReceiveBlobAsync>d__176`1[System.Net.Security.AsyncReadWriteAdapter].MoveNext()",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.__Canon,System.Net.Security.SslStream+<ReceiveBlobAsync>d__176`1[System.Net.Security.AsyncReadWriteAdapter]].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.__Canon,System.Net.Security.SslStream+<ReceiveBlobAsync>d__176`1[System.Net.Security.AsyncReadWriteAdapter]].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.__Canon,System.Net.Security.SslStream+<ReceiveBlobAsync>d__176`1[System.Net.Security.AsyncReadWriteAdapter]].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Threading.Tasks.Task`1[System.Int32].TrySetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.Int32].SetExistingTaskResult(class System.Threading.Tasks.Task`1<!0>,!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1[System.Int32].SetResult(!0)",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Security.SslStream+<<FillHandshakeBufferAsync>g__InternalFillHandshakeBufferAsync|189_0>d`1[System.Net.Security.AsyncReadWriteAdapter].MoveNext()",
          Module ="System.Net.Security.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Int32,System.Net.Security.SslStream+<<FillHandshakeBufferAsync>g__InternalFillHandshakeBufferAsync|189_0>d`1[System.Net.Security.AsyncReadWriteAdapter]].ExecutionContextCallback(class System.Object) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Int32,System.Net.Security.SslStream+<<FillHandshakeBufferAsync>g__InternalFillHandshakeBufferAsync|189_0>d`1[System.Net.Security.AsyncReadWriteAdapter]].MoveNext(class System.Threading.Thread) {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Int32,System.Net.Security.SslStream+<<FillHandshakeBufferAsync>g__InternalFillHandshakeBufferAsync|189_0>d`1[System.Net.Security.AsyncReadWriteAdapter]].MoveNext() {QuickJitted}",
          Module ="System.Private.CoreLib.il"
        },
        new SentryStackFrame() {
          Function ="System.Net.Sockets.SocketAsyncEventArgs+<>c.<.cctor>b__179_0(unsigned int32,unsigned int32,value class System.Threading.NativeOverlapped*)",
          Module ="System.Net.Sockets.il"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="Program.<Main>() {QuickJitted}",
          Module ="Sentry.Samples.Console.Customized"
        },
        new SentryStackFrame() {
          Function ="System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()",
          Module = "System.Private.CoreLib.il"
        }
    };
}
