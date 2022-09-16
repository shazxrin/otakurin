using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Users.Account;
using System.Security.Claims;

namespace Otakurin.Application.Pages.Home.Settings;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;    

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized();
        }

        var user = await _mediator.Send(new GetUserQuery() 
        {
            UserId = Guid.Parse(userIdClaim.Value) 
        });

        Email = user.Email;
        UserName = user.UserName;

        return Page();
    }
}