using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Otakurin.Application.Configurations;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Users.Account;

namespace Otakurin.Application.Pages.Auth;

public class SignInModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly SecretKeyConfiguration _secretKeyConfiguration;

    [BindProperty]
    [Required]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Key1 { get; set; } = string.Empty;
    
    [BindProperty]
    [Required]
    public string Key2 { get; set; } = string.Empty;
    
    [BindProperty]
    [Required]
    public string Key3 { get; set; } = string.Empty;

    public SignInModel(IMediator mediator, IOptions<SecretKeyConfiguration> secretKeyOptions)
    {
        _mediator = mediator;
        _secretKeyConfiguration = secretKeyOptions.Value;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return LocalRedirect("/Home");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Username or password or keys invalid!");

            return Page();
        }

        if (!(Key1.Equals(_secretKeyConfiguration.Key1) && Key2.Equals(_secretKeyConfiguration.Key2) &&
            Key3.Equals(_secretKeyConfiguration.Key3)))
        {
            ModelState.AddModelError(string.Empty, "Username or password or keys invalid!");

            return Page();
        }

        try
        {
            var result = await _mediator.Send(new SignInUserCommand()
            {
                UserName = UserName,
                Password = Password
            });

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
                new(ClaimTypes.Name, result.User.UserName),
                new(ClaimTypes.Email, result.User.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                IssuedUtc = DateTimeOffset.Now
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties
            );

            return LocalRedirect(Url.Content(returnUrl) ?? "/Home");
        }
        catch (ForbiddenException)
        {
            ModelState.AddModelError(string.Empty, "Username or password or keys invalid!");

            return Page();
        }
    }
}