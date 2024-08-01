/*
 * This sample demonstrates how to instrument graphql-dotnet via Open Telemetry and have that
 * trace information sent to Sentry.
 */

using System.Text.Json;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using GraphQL.MicrosoftDI;
using GraphQL.Telemetry;
using GraphQL.Types;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.Samples.GraphQL.Server.Notes;

namespace Sentry.Samples.GraphQL.Server;

public static class Program
{
    public static void Main(string[] args)
    {
        BuildWebApplication(args).Run();
    }

    // public static IWebHost BuildWebHost(string[] args) =>
    public static WebApplication BuildWebApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddSource(GraphQLTelemetryProvider.SourceName)  // <-- Ensure telemetry is gathered from graphql
                    .ConfigureResource(resource => resource.AddService("Sentry.Samples.GraphQL.Server"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSentry() // <-- Ensure telemetry is sent to Sentry
                );

        builder.WebHost.UseSentry(options =>
        {
            // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

            options.TracesSampleRate = 1.0;
            options.Debug = true;
            options.SendDefaultPii = true;
            options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
        });

        builder.Services
            // Add our data store
            .AddSingleton<NotesData>()
            // add notes schema
            .AddSingleton<ISchema, NotesSchema>(services =>
                new NotesSchema(new SelfActivatingServiceProvider(services))
            )
            // register graphQL
            .AddGraphQL(options => options
                .AddAutoSchema<NotesSchema>()
                .AddSystemTextJson()
                .UseTelemetry(telemetryOptions =>
                {
                    telemetryOptions.RecordDocument = true; // <-- Configure GraphQL to use OpenTelemetry
                    telemetryOptions.EnrichWithExecutionResult = (activity, _, executionResult) =>
                    {
                        // An example of how you can capture additional information to send to Sentry.
                        // Here we show how to capture the errors that get returned to GraphQL clients
                        // and add them as tags on the Activity, which will show in Sentry as `otel`
                        // context.
                        if (executionResult.Errors is not { } errors)
                        {
                            return;
                        }

                        if (errors.Count == 1)
                        {
                            activity.AddTag("Error", executionResult.Errors[0].Message);
                        }
                        else
                        {
                            for (var i = 0; i < errors.Count; i++)
                            {
                                activity.AddTag($"Errors[{i}]", executionResult.Errors[i].Message);
                            }
                        }
                    };
                })
            );

        // Permit any Origin - not appropriate for production!!!
        builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.WithOrigins("*").AllowAnyHeader()));
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Sentry.Samples.GraphQL",
                Version = "v1"
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sentry.Samples.GraphQL v1"));
            app.UseGraphQLAltair(); // Exposed at /ui/altair
        }

        app.UseAuthorization();
        app.MapControllers();

        app.MapGet("/", () => "Hello world!");
        app.MapGet("/request", async context =>
        {
            var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/graphql";
            var graphClient = new GraphQLHttpClient(url, new SystemTextJsonSerializer());
            var notesRequest = new GraphQLRequest
            {
                Query = @"
                {
                  notes {
                    id,
                    message
                  }
                }"
            };
            var graphResponse = await graphClient.SendQueryAsync<NotesResult>(notesRequest);
            var result = JsonSerializer.Serialize(graphResponse.Data);
            await context.Response.WriteAsync(result);
        });
        app.MapGet("/throw", () => { throw new NotImplementedException(); });

        // make sure all our schemas registered to route
        app.UseGraphQL("/graphql");

        return app;
    }

    public class NotesResult
    {
        public List<Note> Notes { get; set; } = new();
    }
}
