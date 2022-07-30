using Serilog;
using Serilog.Exceptions;

namespace Otakurin.Application.Extensions;

public static class LoggerHostExtensions
{
    public static void ConfigLogger(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetService<IWebHostEnvironment>();

        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console()
            .MinimumLevel.Information();
        
        Log.Logger = logger.CreateLogger();     
    }
}