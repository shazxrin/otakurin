using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Content;
using Otakurin.Core.Games.Tracking;
using Otakurin.Core.Games.Wishlist;

namespace Otakurin.Application.Pages.Home.Share;

public class GameModel : PageModel
{
    private readonly IMediator _mediator;
    
    [BindProperty(SupportsGet = true)]
    public Guid GameId { get; set; } = Guid.Empty;

    [BindProperty(SupportsGet = true)] 
    public string Platform { get; set; } = string.Empty;

    [BindProperty] 
    public GetGameResult Game { get; private set; } = new();

    [BindProperty] 
    public GetGameTrackingResult GameTracking { get; private set; } = new();

    public GameModel(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<IActionResult> OnGet()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {

            return Unauthorized();
        }
            
        try
        {
            var gameResult = await _mediator.Send(new GetGameQuery()
            {
                GameId = GameId
            });
            Game = gameResult;

            var trackingResult = await _mediator.Send(new GetGameTrackingQuery()
            {
                UserId = Guid.Parse(userIdClaim.Value),
                GameId = GameId,
                Platform = Platform
            });
            GameTracking = trackingResult;

            return Page();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}