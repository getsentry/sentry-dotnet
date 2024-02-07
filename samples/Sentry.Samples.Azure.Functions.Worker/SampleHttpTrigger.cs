using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.Azure.Functions.Worker;

public class SampleHttpTrigger
{
    [Function(nameof(SampleHttpTrigger))]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<SampleHttpTrigger>();
        logger.LogInformation("C# HTTP trigger function processed a request");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        BadApple.HttpScenario();

        await response.WriteStringAsync("Welcome to Azure Functions!");

        return response;

    }
}
