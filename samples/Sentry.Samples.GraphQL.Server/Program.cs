using System.Text.Json;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using GraphQL.MicrosoftDI;
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
                    .AddSource(Telemetry.ActivitySource.Name)
                    .ConfigureResource(resource => resource.AddService(Telemetry.ServiceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSentry()
                ); // <-- Configure OpenTelemetry to send traces to Sentry

        builder.WebHost.UseSentry(o =>
        {
            // A DSN is required.  You can set it here, or in configuration, or in an environment variable.
            // o.Dsn = "...Your DSN Here...";
            o.EnableTracing = true;
            o.Debug = true;
            o.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
        });

        builder.Services
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
