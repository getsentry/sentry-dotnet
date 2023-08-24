using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Sentry.AspNetCore.Blazor.Tests;

public class WasmStackTraceFactoryTests
{
    [Theory]
    [MemberData(nameof(GetStackTraceTestCases))]
    public void TryParse(
        SentryBlazorOptions options,
        string line,
        string instrAddr,
        string platform,
        string addressMode,
        string fileName,
        bool parsed)
    {
        var mock = Substitute.For<ISentryStackTraceFactory>();
        var sut = new WasmStackTraceFactory(mock, options);
        Assert.Equal(parsed, sut.TryParse(line, out var result));

        Assert.NotNull(result);
        Assert.Equal(instrAddr, result.InstructionAddress);
        Assert.Equal(platform, result.Platform);
        Assert.Equal(addressMode, result.AddressMode);
        Assert.Equal(fileName, result.FileName);
    }

    public static IEnumerable<object[]> GetStackTraceTestCases()
    {
        var options = new SentryBlazorOptions();
        options.GetDebugImages = () => new DebugImage[]
        {
            new()
            {
                Type = "wasm",
                CodeId = "codeid",
                CodeFile = "wasm://wasm/009931b2",
                DebugFile = "debug file",
                DebugId = "build_id",
            }
        };
        yield return new object[]
        {
            options, "    at wasm://wasm/009931b2:wasm-function[313]:0x1d6b6", "0x1d6b6", "wasm", "rel:0", "wasm://wasm/009931b2", true
        };
    }


    private const string stackTraceString2 =
        @"RuntimeError: Aborted(). Build with -sASSERTIONS for more info.
    at abort (dotnet.7.0.10.po24o75x0q.js:14:13301)
    at _abort (dotnet.7.0.10.po24o75x0q.js:14:95254)
    at dotnet.wasm:0x1a7939
    at dotnet.wasm:0x3dcaa
    at dotnet.wasm:0x1c03e8
    at eval (eval at <anonymous> (127.0.0.1/:1:1), <anonymous>:1:26)
    at (index):51:21
    at async blazor.webassembly.js:1:45568
    at async blazor.webassembly.js:1:45359";

            const string stackTraceString = @"Error: Exceptional JavaScript function called by: from C#
    at throwFromJavaScript (https://localhost:5001/:46:15)
    at https://localhost:5001/_framework/blazor.webassembly.js:1:3337
    at new Promise (<anonymous>)
    at Object.beginInvokeJSFromDotNet (https://localhost:5001/_framework/blazor.webassembly.js:1:3311)
    at Object.Gt [as invokeJSFromDotNet] (https://localhost:5001/_framework/blazor.webassembly.js:1:62569)
    at Object.Ii (https://localhost:5001/_framework/dotnet.7.0.10.hef7nl7p9e.js:5:71974)
    at _mono_wasm_invoke_js_blazor (https://localhost:5001/_framework/dotnet.7.0.10.hef7nl7p9e.js:14:103886)
    at wasm://wasm/009931b2:wasm-function[283]:0x1cae4
    at wasm://wasm/009931b2:wasm-function[221]:0xe1d4
    at wasm://wasm/009931b2:wasm-function[220]:0xd044
    at wasm://wasm/009931b2:wasm-function[8115]:0x1a2377
    at wasm://wasm/009931b2:wasm-function[1395]:0x6889a
    at wasm://wasm/009931b2:wasm-function[283]:0x1cae4
    at wasm://wasm/009931b2:wasm-function[221]:0xe1d4
    at wasm://wasm/009931b2:wasm-function[220]:0xd044
    at wasm://wasm/009931b2:wasm-function[8115]:0x1a2377
    at wasm://wasm/009931b2:wasm-function[2072]:0x8826d
    at wasm://wasm/009931b2:wasm-function[114]:0x9d80
    at Module._mono_wasm_invoke_method_ref (https://localhost:5001/_framework/dotnet.7.0.10.hef7nl7p9e.js:14:123869)
    at _Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet (https://dotnet.generated.invalid/_Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet:29:5)
    at Object.beginInvokeDotNetFromJS (https://localhost:5001/_framework/blazor.webassembly.js:1:45087)
    at b (https://localhost:5001/_framework/blazor.webassembly.js:1:1998)
    at A.invokeMethodAsync (https://localhost:5001/_framework/blazor.webassembly.js:1:3866)
    at https://localhost:5001/_framework/blazor.webassembly.js:1:11414
    at Object.invokeWhenHeapUnlocked (https://localhost:5001/_framework/blazor.webassembly.js:1:47333)
    at S (https://localhost:5001/_framework/blazor.webassembly.js:1:58698)
    at A (https://localhost:5001/_framework/blazor.webassembly.js:1:11383)
    at O.dispatchGlobalEventToAllElements (https://localhost:5001/_framework/blazor.webassembly.js:1:13968)
    at O.onGlobalEvent (https://localhost:5001/_framework/blazor.webassembly.js:1:13161)
    at HTMLDocument.sentryWrapped (https://localhost:5001/_content/Sentry.AspNetCore.Blazor/bundle.tracing.replay.debug.min.js:2:210673)";

            // Published app with 'BlazorCacheBootResources' false.
            private const string stackTraceEx =
                @"      Unhandled exception rendering component: Exceptional JavaScript function called by: from C#
      Error: Exceptional JavaScript function called by: from C#
          at throwFromJavaScript (http://127.0.0.1:8080/:149:15)
          at http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:3337
          at new Promise (<anonymous>)
          at Object.beginInvokeJSFromDotNet (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:3311)
          at Object.Gt [as invokeJSFromDotNet] (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:62569)
          at Object.Ii (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:5:71974)
          at _mono_wasm_invoke_js_blazor (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:14:102842)
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[204]:0x1827c
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[174]:0x176ad
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[112]:0x724f
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[111]:0x60c4
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7602]:0x189cf3
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[1619]:0x6f3c9
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[1617]:0x6f33f
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[1052]:0x51591
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[204]:0x18235
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[174]:0x176ad
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[112]:0x724f
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[111]:0x60c4
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7602]:0x189cf3
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[1619]:0x6f3c9
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[1624]:0x6faf2
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[1651]:0x71ba1
          at http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[8534]:0x1c134b
          at Module._mono_wasm_invoke_method_ref (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:14:122299)
          at _Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet (https://dotnet.generated.invalid/_Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet:29:5)
          at Object.beginInvokeDotNetFromJS (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:45087)
          at b (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:1998)
          at A.invokeMethodAsync (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:3866)
          at http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:11414
          at Object.invokeWhenHeapUnlocked (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:47333)
          at S (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:58698)
          at A (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:11383)
          at O.dispatchGlobalEventToAllElements (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:13968)
          at O.onGlobalEvent (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:13161)
          at HTMLDocument.sentryWrapped (http://127.0.0.1:8080/_content/Sentry.AspNetCore.Blazor/bundle.tracing.replay.debug.min.js:2:210673)";

            // Options: BlazorCacheBootResources=false, WasmNativeDebugSymbols=true, RunAOTCompilation=true, RunAOTCompilationAfterBuild=true, WasmNativeStrip=false
            private const string ExceptionWithFuncNames = @"Error: Exceptional JavaScript function called by: from C#
    at throwFromJavaScript (http://127.0.0.1:8080/:151:15)
    at http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:3337
    at new Promise (<anonymous>)
    at Object.beginInvokeJSFromDotNet (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:3311)
    at Object.Gt [as invokeJSFromDotNet] (http://127.0.0.1:8080/_framework/blazor.webassembly.js:1:62569)
    at Object.Ii (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:5:71974)
    at _mono_wasm_invoke_js_blazor (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:5962:71)
    at do_icall (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57973]:0xaea8b8)
    at do_icall_wrapper (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57920]:0xae9872)
    at interp_exec_method (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57849]:0xadb64b)
    at interp_entry (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57902]:0xae91a5)
    at interp_entry_instance_5 (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57995]:0xaeb62e)
    at Microsoft_JSInterop_aot_wrapper_gsharedvt_in_sig_void_this_i8objobji4i8 (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7678]:0x1b271b)
    at dynCall_vijiiiji (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[74600]:0xcdf31f)
    at legalstub$dynCall_vijiiiji (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[74715]:0xcdfcd1)
    at invoke_vijiiiji (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:8625:5)
    at legalfunc$invoke_vijiiiji (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[74807]:0xce0949)
    at Microsoft_JSInterop_Microsoft_JSInterop_JSRuntime_InvokeAsync_TValue_REF_long_string_System_Threading_CancellationToken_object__ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7144]:0x18b2ac)
    at dynCall_viijiiii (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[74595]:0xcdf2b5)
    at legalstub$dynCall_viijiiii (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[74710]:0xcdfc2c)
    at invoke_viijiiii (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:8636:5)
    at legalfunc$invoke_viijiiii (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[74808]:0xce0966)
    at Microsoft_JSInterop_Microsoft_JSInterop_JSRuntime__InvokeAsyncd__16_1_TValue_REF_MoveNext (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7160]:0x18cd3e)
    at corlib_aot_wrapper_gsharedvt_out_sig_pinvoke_void_this_ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[54482]:0xa9349b)
    at jit_call_cb (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57976]:0xaeaf0b)
    at invoke_vi (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:8075:29)
    at mono_llvm_cpp_catch_exception (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[58441]:0xb07c13)
    at do_jit_call (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57923]:0xae9cea)
    at interp_exec_method (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57849]:0xadb9f0)
    at interp_runtime_invoke (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57848]:0xada4db)
    at mono_jit_runtime_invoke (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[72627]:0xc9a160)
    at do_runtime_invoke (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[60640]:0xb4c9bf)
    at mono_runtime_invoke_checked (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[60638]:0xb4c967)
    at mono_gsharedvt_constrained_call (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[72766]:0xc9e58a)
    at aot_wrapper_icall_mono_gsharedvt_constrained_call (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[50654]:0x9de283)
    at corlib_System_Runtime_CompilerServices_AsyncMethodBuilderCore_Start_TStateMachine_GSHAREDVT_TStateMachine_GSHAREDVT_ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[50966]:0x9ed8e6)
    at corlib_System_Runtime_CompilerServices_AsyncValueTaskMethodBuilder_1_TResult_GSHAREDVT_Start_TStateMachine_GSHAREDVT_TStateMachine_GSHAREDVT_ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[50998]:0x9eea87)
    at Microsoft_JSInterop_Microsoft_JSInterop_JSRuntime_InvokeAsync_TValue_REF_long_string_object__ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7143]:0x18aac4)
    at Microsoft_JSInterop_Microsoft_JSInterop_JSRuntime_InvokeAsync_TValue_REF_string_object__ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7142]:0x18a8fd)
    at Microsoft_JSInterop_Microsoft_JSInterop_JSRuntimeExtensions_InvokeAsync_TValue_REF_Microsoft_JSInterop_IJSRuntime_string_object__ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[7165]:0x18d9eb)
    at invoke_viiiii (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:8086:29)
    at Sentry_Samples_AspNetCore_Blazor_Wasm_Sentry_Samples_AspNetCore_Blazor_Wasm_Pages_Thrower__ThrowFromJavaScriptd__3_MoveNext (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[15928]:0x34f8d0)
    at corlib_aot_wrapper_gsharedvt_out_sig_pinvoke_void_this_ (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[54482]:0xa9349b)
    at jit_call_cb (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57976]:0xaeaf0b)
    at invoke_vi (http://127.0.0.1:8080/_framework/dotnet.7.0.10.po24o75x0q.js:8075:29)
    at mono_llvm_cpp_catch_exception (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[58441]:0xb07c13)
    at do_jit_call (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57923]:0xae9cea)
    at interp_exec_method (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57849]:0xadb9f0)
    at interp_runtime_invoke (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[57848]:0xada4db)
    at mono_jit_runtime_invoke (http://127.0.0.1:8080/_framework/dotnet.wasm:wasm-function[72627]:0xc9a160)";

            // Options: BlazorCacheBootResources=false, WasmNativeDebugSymbols=true, RunAOTCompilation=true, RunAOTCompilationAfterBuild=true, WasmNativeStrip=true
            private const string ExceptionStripTrue = @"Error: Exceptional JavaScript function called by: from C#
    at throwFromJavaScript (http://localhost:5000/:151:15)
    at http://localhost:5000/_framework/blazor.webassembly.js:1:3337
    at new Promise (<anonymous>)
    at Object.beginInvokeJSFromDotNet (http://localhost:5000/_framework/blazor.webassembly.js:1:3311)
    at Object.Gt [as invokeJSFromDotNet] (http://localhost:5000/_framework/blazor.webassembly.js:1:62569)
    at Object.Ii (http://localhost:5000/_framework/dotnet.7.0.10.kqfmmnscx9.js:5:71974)
    at _mono_wasm_invoke_js_blazor (http://localhost:5000/_framework/dotnet.7.0.10.kqfmmnscx9.js:6062:71)
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[232357]:0xb94c9a2
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[18623]:0xdd362e
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[250089]:0xbb7af34
    at invoke_vijiiiji (http://localhost:5000/_framework/dotnet.7.0.10.kqfmmnscx9.js:9888:5)
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[250224]:0xbb7c224
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[17971]:0xd1acce
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[250083]:0xbb7ae72
    at invoke_viijiiii (http://localhost:5000/_framework/dotnet.7.0.10.kqfmmnscx9.js:9899:5)
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[250225]:0xbb7c241
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[18110]:0xd54993
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[232338]:0xb94c16e
    at invoke_vi (http://localhost:5000/_framework/dotnet.7.0.10.kqfmmnscx9.js:9085:29)
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[232803]:0xb96aa58
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[17970]:0xd1890a
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[17968]:0xd180b5
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[17988]:0xd217f7
    at invoke_viiiii (http://localhost:5000/_framework/dotnet.7.0.10.kqfmmnscx9.js:9129:29)
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[264]:0xf9189
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[200309]:0xa318b1d
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[229]:0xf268b
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[211869]:0xabe7858
    at http://localhost:5000/_framework/dotnet.wasm:wasm-function[1444]:0x1bdbeb";
}
