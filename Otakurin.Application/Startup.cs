using Microsoft.AspNetCore.CookiePolicy;
using Otakurin.Application.Extensions;
using Serilog;

namespace Otakurin.Application;

public class Startup
{
    private readonly IConfiguration _configuration;
        
    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWebServices();        
        services.AddLoggerServices();
        services.AddDatabaseServices(_configuration);
        services.AddExternalAPIServices(_configuration);
        services.AddIdentityServices(_configuration);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSerilogRequestLogging();
            
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCookiePolicy(new CookiePolicyOptions()
        {
            HttpOnly = HttpOnlyPolicy.Always,
            Secure = CookieSecurePolicy.Always,
            MinimumSameSitePolicy = SameSiteMode.Strict
        });

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseStaticFiles();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
        });
    }
}
