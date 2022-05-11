using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Sentry.Samples.AspnetCore21.Basic
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async context =>
            {
                // "throw" anywhere on the URL will trigger an exception that will get sent to Sentry.

                if (context.Request.Path.ToString().Contains("throw"))
                {
                    throw new Exception("An exception thrown from the ASP.NET Core pipeline");
                }

                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
