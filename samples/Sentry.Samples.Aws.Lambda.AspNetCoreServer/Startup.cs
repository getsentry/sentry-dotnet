using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public static IConfiguration Configuration { get; private set; }

    public void ConfigureServices(IServiceCollection services) => services.AddControllers();

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(e => e.MapControllers());
    }
}
