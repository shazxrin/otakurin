using Serilog;

namespace Otakurin.Application.Extensions;

public static class LoggerServiceExtensions
{
    public static void AddLoggerServices(this IServiceCollection services)
    {
        services.AddSingleton<Serilog.ILogger>(Log.Logger);
    }
}