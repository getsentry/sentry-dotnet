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
    at dotnet.wasm:0x1a76e8
    at dotnet.wasm:0x1a79ad
    at dotnet.wasm:0x1a7841
    at dotnet.wasm:0x1a78d0
    at dotnet.wasm:0x1a7939
    at dotnet.wasm:0x44426
    at dotnet.wasm:0x3e74c
    at dotnet.wasm:0x400fd
    at dotnet.wasm:0x3ec74
    at dotnet.wasm:0x3ce8e
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
    at wasm://wasm/009931b2:wasm-function[313]:0x1d6b6
    at wasm://wasm/009931b2:wasm-function[283]:0x1cae4
    at wasm://wasm/009931b2:wasm-function[221]:0xe1d4
    at wasm://wasm/009931b2:wasm-function[220]:0xd044
    at wasm://wasm/009931b2:wasm-function[8115]:0x1a2377
    at wasm://wasm/009931b2:wasm-function[2040]:0x85b46
    at wasm://wasm/009931b2:wasm-function[2038]:0x85abc
    at wasm://wasm/009931b2:wasm-function[1395]:0x6889a
    at wasm://wasm/009931b2:wasm-function[313]:0x1d66f
    at wasm://wasm/009931b2:wasm-function[283]:0x1cae4
    at wasm://wasm/009931b2:wasm-function[221]:0xe1d4
    at wasm://wasm/009931b2:wasm-function[220]:0xd044
    at wasm://wasm/009931b2:wasm-function[8115]:0x1a2377
    at wasm://wasm/009931b2:wasm-function[2040]:0x85b46
    at wasm://wasm/009931b2:wasm-function[2045]:0x861ae
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
}
