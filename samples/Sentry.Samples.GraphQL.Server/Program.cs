using GraphQL.MicrosoftDI;
using GraphQL.Server;
using GraphQL.Types;
using Sentry.Samples.GraphQL.Server.Notes;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(o =>
    {
        // A DSN is required.  You can set it here, or in configuration, or in an environment variable.
        o.Dsn = "...Your DSN Here...";

        // Enable Sentry performance monitoring
        o.EnableTracing = true;

#if DEBUG
        // Log debug information about the Sentry SDK
        o.Debug = true;
#endif
    });

// Add services to the container.
// add notes schema
builder.Services.AddSingleton<ISchema, NotesSchema>(services => new NotesSchema(new SelfActivatingServiceProvider(services)));
// register graphQL
builder.Services.AddGraphQL(options =>
    {
        options.EnableMetrics = true;
    })
    .AddSystemTextJson();
// default setup
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GraphQLNetExample", Version = "v1" });
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
app.MapGet("/", () => "Hello World!");
app.MapGet("/throw", () => { throw new NotImplementedException(); });

// make sure all our schemas registered to route
app.UseGraphQL<ISchema>();

app.Run();
