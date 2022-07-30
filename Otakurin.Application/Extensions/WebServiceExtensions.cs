using System.Reflection;
using MediatR;

namespace Otakurin.Application.Extensions;

public static class WebServiceExtensions
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/Home");
        });

        var coreAssembly = Assembly.GetAssembly(typeof(Core.Core));
        if (coreAssembly != null)
        {
            services.AddMediatR(coreAssembly);
            services.AddAutoMapper(coreAssembly);
        }
    }
}