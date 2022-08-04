using Otakurin.Application.Extensions;
using Serilog;

namespace Otakurin.Application;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        host.ConfigLogger();
     
        Log.Logger.Information(@"

             ██████╗ ████████╗ █████╗ ██╗  ██╗██╗   ██╗██████╗ ██╗███╗   ██╗
            ██╔═══██╗╚══██╔══╝██╔══██╗██║ ██╔╝██║   ██║██╔══██╗██║████╗  ██║
            ██║   ██║   ██║   ███████║█████╔╝ ██║   ██║██████╔╝██║██╔██╗ ██║
            ██║   ██║   ██║   ██╔══██║██╔═██╗ ██║   ██║██╔══██╗██║██║╚██╗██║
            ╚██████╔╝   ██║   ██║  ██║██║  ██╗╚██████╔╝██║  ██║██║██║ ╚████║
             ╚═════╝    ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝

        ");
        
        host.ConfigDatabase();
        
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Fatal(ex, "Host terminated unexpectedly!");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options => options.AddServerHeader = false);
                webBuilder.UseStartup<Startup>();
            });
}
