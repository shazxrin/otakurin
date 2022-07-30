using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Content;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Tracking;

namespace Otakurin.Application.Pages.Home.Settings;

public class ImportModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty] 
    public string Data { get; set; }

    [BindProperty] 
    public bool IsSuccess { get; set; } = false;

    public ImportModel(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<IActionResult> OnPost()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {

            return Unauthorized();
        }

        try
        {
            string[] entries = Data.Split("\n");

            foreach (var entry in entries)
            {
                string[] items = entry.Split(", ");

                var gameResult = await _mediator.Send(new FetchGameCommand
                {
                    GameRemoteId = long.Parse(items[0])
                });

                await _mediator.Send(new AddGameTrackingCommand
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = gameResult.GameId,
                    HoursPlayed = int.Parse(items[2]),
                    Platform = items[3],
                    Format = Enum.Parse<MediaTrackingFormat>(items[4]),
                    Status = Enum.Parse<MediaTrackingStatus>(items[5]),
                    Ownership = Enum.Parse<MediaTrackingOwnership>(items[6])
                });
            }
        }
        catch (ExistsException)
        {
            ModelState.AddModelError("exists", "Please clear existing trackings!");

            return Page();
        }
        
        IsSuccess = true;
        return Page();
    }
}