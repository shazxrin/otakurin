using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Otakurin.Core.Services;
using Otakurin.Domain.User;
using Otakurin.Persistence;

namespace Otakurin.Application.Extensions;

public static class IdentityServiceExtensions
{
    public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddIdentityCore<UserAccount>()
            .AddEntityFrameworkStores<DatabaseContext>();
        
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Auth/SignIn";
                options.LoginPath = "/Auth/SignIn";
                options.LogoutPath = "/Index";
            });

        services.AddAuthorization();
    }
}