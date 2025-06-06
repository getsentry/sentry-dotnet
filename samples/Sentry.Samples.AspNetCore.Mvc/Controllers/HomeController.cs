using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Samples.AspNetCore.Mvc.Models;
using Sentry.Ben.BlockingDetector;
using Sentry.Samples.AspNetCore.Mvc.Models;

namespace Samples.AspNetCore.Mvc.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("[controller]/sampler")]
    public Task<string> Sampler()
    {
        // The sampling for this route is determined by the TraceSampler function configured in Program.cs when
        // initializing the Sentry SDK... here we just display the result of that sampling decision.
        var transaction = SentrySdk.GetSpan()?.GetTransaction();
        return Task.FromResult($"Sampled: {transaction?.IsSampled}");
    }

    // GET /home/block/true or /home/block/false to observe events
    [HttpGet("[controller]/block/{block?}")]
    public async Task<string> Block([FromRoute] bool block)
    {
        if (block)
        {
            logger.LogInformation("\ud83d\ude31 Calling a blocking API on an async method \ud83d\ude31");

            // This will result in an event in Sentry
            Task.Delay(10).Wait(); // This is a blocking call. Same with '.Result'
        }
        else
        {
            logger.LogInformation("\ud83d\ude31 No blocking call made \ud83d\ude31");

            // Non-blocking, no event captured
            await Task.Delay(10);
        }

        return "Was blocking? " + block;
    }

    // GET /home/suppress/true or /home/suppress/false to observe events
    [HttpGet("[controller]/suppress/{suppress?}")]
    public async Task<string> Suppress([FromRoute] bool suppress)
    {
        if (suppress)
        {
            logger.LogInformation("Blocking suppression enabled");
            using (new SuppressBlockingDetection())
            {
                Task.Delay(10).Wait(); // This is blocking but won't trigger an event, due to suppression
            }
            logger.LogInformation("Blocking suppression disabled");
        }
        else
        {
            logger.LogInformation("\ud83d\ude31 Unsuppressed blocking call on an async method \ud83d\ude31");
            Task.Delay(10).Wait(); // This is blocking but won't trigger an event, due to suppression
        }

        // Non-blocking, no event captured
        await Task.Delay(10);

        return "Was suppressed? " + suppress;
    }

    // Example: An exception that goes unhandled by the app will be captured by Sentry:
    [HttpPost]
    public async Task PostIndex(string? @params)
    {
        try
        {
            if (@params == null)
            {
                // This will get captured in Sentry as an event (MinimumEventLevel is Warning in appsettings.json)
                logger.LogWarning("Param is null!");
            }

            // This will get captured as a Breadcrumb (MinimumBreadcrumbLevel is Information in appsettings.json)
            logger.LogInformation("Completing some tasks in parallel");

            var firstTask = Task.Run(new Func<int>(() => throw new HttpRequestException("Failed to complete task 1")));
            var secondTask = Task.Run(new Func<int>(() => throw new DataException("Invalid data for task 2")));

            var whenAll = Task.WhenAll(firstTask, secondTask);
            try
            {
                var ids = await whenAll;
                logger.LogInformation("Completed tasks: {DungeonId}, {ManaId}", ids[0], ids[1]);
            }
            catch when (whenAll.Exception is { InnerExceptions.Count: > 1 } ae)
            {
                // await unwraps AggregateException and throws the first one by default. Here we make sure to throw the
                // original AggregateException to ensure all errors are captured
                throw ae;
            }
        }
        catch (Exception e)
        {
            var ioe = new InvalidOperationException("Bad POST! See Inner exception for details", e);

            ioe.Data.Add("thoughts",
                // This is an example of sending some additional information with the exception... The following
                // anonymous object will be serialized and sent with the exception to Sentry as additional context.
                new
                {
                    Question = "What is the meaning of life?",
                    Answer = 42
                });

            throw ioe;
        }
    }

    // Example: The view rendering throws: see /Views/Home/Razor.cshtml
    public IActionResult Razor(string? who)
    {
        if (who == null)
        {
            // Exemplifies using the logger to raise a warning which will be sent as an event because MinimumEventLevel was configured to Warning
            // ALso, the stack trace of this location will be sent (even though there was no exception) because of the configuration AttachStackTrace
            logger.LogWarning("A {route} '{value}' was requested.",
                // example structured logging where keys (in the template above) go as tag keys and values below:
                "/razor",
                "null");
        }

        return View();
    }

    public IActionResult Capture(
        // IHub holds a Client and Scope management. Errors sent via IHub will include any context from the current scope
        [FromServices] IHub sentry)
    {
        // Anything we change on the scope here will be sent with any subsequent events to Sentry
        sentry.ConfigureScope(scope =>
        {
            // This will show up under "Additional data" in the event details on Sentry
            scope.SetExtra("Some additional data", "Be good to your mother");
            // You can add tags too
            scope.SetTag("Tag", "You're it!");
        });
        try
        {
            // Some code block that could throw
            throw null!;
        }
        catch (Exception e)
        {
            e.Data.Add("detail",
                new
                {
                    Reason = "There's a 'throw null' hard-coded here!",
                    IsCrazy = true
                });

            var id = sentry.CaptureException(e);

            ViewData["Message"] = "An exception was caught and sent to Sentry! Event ID: " + id;
        }
        return View();
    }

    public IActionResult Action()
    {
        throw new Exception("Test exception thrown from a controller action method");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("[controller]/feedback")]
    public IActionResult Feedback()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitFeedback(FeedbackModel feedback)
    {
        if (!ModelState.IsValid)
        {
            return View("Feedback", feedback);
        }

        var sentryFeedback = new SentryFeedback(feedback.Message!, feedback.ContactEmail, feedback.Name);
        var hint = new SentryHint();

        if (feedback.Screenshot is { Length: > 0 })
        {
            await using var memoryStream = new MemoryStream();
            await feedback.Screenshot.CopyToAsync(memoryStream);
            hint.AddAttachment(memoryStream.ToArray(), feedback.Screenshot.FileName, AttachmentType.Default, "image/png");
        }

        SentrySdk.CaptureFeedback(sentryFeedback, hint: hint);
        ViewBag.Message = "Feedback submitted successfully!";
        return View("Index");
    }
}
