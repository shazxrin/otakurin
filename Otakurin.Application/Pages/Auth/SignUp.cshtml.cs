using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Users.Account;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Application.Pages.Auth;

public class SignUpModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [BindProperty]
    [Required]
    [MinLength(6)]
    [MaxLength(20)]
    [RegularExpression(
        "^[a-zA-Z0-9]+$",
        ErrorMessage = "The field UserName can only contain alphanumeric characters."
    )]
    public string UserName { get; set; } = string.Empty;
    
    [BindProperty]
    [Required]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{6,}$", 
        ErrorMessage = "The field Password must have at least 1 of these: capital, non-capital, number and symbol."
    )]
    public string Password { get; set; } = string.Empty;

    public SignUpModel(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return LocalRedirect("/Home");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Check fields!");

            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _mediator.Send(new SignUpUserCommand()
            {
                Email = Email,
                UserName = UserName,
                Password = Password
            });

            return LocalRedirect("/Auth/SignIn");
        }
        catch (ExistsException e)
        {
            ModelState.AddModelError(string.Empty, e.Message);

            return Page();
        }
        catch (ValidationException e)
        {
            ModelState.AddModelError(string.Empty, e.Message);

            return Page();
        }
    }
}