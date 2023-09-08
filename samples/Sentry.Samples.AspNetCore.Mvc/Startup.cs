using Sentry.Extensibility;

namespace Samples.AspNetCore.Mvc;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Register as many ISentryEventExceptionProcessor as you need. They ALL get called.
        services.AddSingleton<ISentryEventExceptionProcessor, SpecialExceptionProcessor>();

        // You can also register as many ISentryEventProcessor as you need.
        services.AddTransient<ISentryEventProcessor, ExampleEventProcessor>();

        services.AddSentryTunneling();

        // To demonstrate taking a request-aware service into the event processor above
        services.AddHttpContextAccessor();

        services.AddSingleton<IGameService, GameService>();

        services.AddMvc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseSentryTunneling();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
