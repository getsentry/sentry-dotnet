using System.Text.Json;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using GraphQL.MicrosoftDI;
using GraphQL.Types;
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

        builder.WebHost.UseSentry(o =>
        {
            // A DSN is required.  You can set it here, or in configuration, or in an environment variable.
            // o.Dsn = "...Your DSN Here...";
            o.EnableTracing = true;
            o.Debug = true;
        });

        // Add services to the container.
        // add notes schema
        builder.Services.AddSingleton<ISchema, NotesSchema>(services =>
            new NotesSchema(new SelfActivatingServiceProvider(services))
        );

        // register graphQL
        builder.Services.AddGraphQL(options => options
            .AddAutoSchema<NotesSchema>()
            .AddSystemTextJson()
        );

        // Permit anything Origin - not appropriate for production
        builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.WithOrigins("*").AllowAnyHeader()));
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "GraphQLNetExample",
                Version = "v1"
            });
        });

        var app = builder.Build();
        app.UseSentryTracing();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GraphQLNetExample v1"));
            // add altair UI to development only
            app.UseGraphQLAltair();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // An example ASP.NET Core middleware that throws an
        // exception when serving a request to path: /throw
        app.MapGet("/", async context =>
        {
            var request = context.Request;
            var url = $"{request.Scheme}://{request.Host}{request.PathBase}/graphql";
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
