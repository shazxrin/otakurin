using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Content;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Tracking;

namespace Otakurin.Application.Pages.Home.Games.Trackings
{
    public class EditModel : PageModel
    {
        private readonly IMediator _mediator;

        public GetGameResult Game { get; private set; } = new();

        public GetGameTrackingResult GameTracking { get; private set; } = new();

        [BindProperty(SupportsGet = true)] 
        public Guid GameId { get; set; } = Guid.Empty;
        
        [BindProperty(SupportsGet = true)] 
        public string Platform { get; set; } = string.Empty;
        
        [BindProperty(SupportsGet = true)] 
        public string ReturnUrl { get; set; } = string.Empty;

        [BindProperty] 
        public int HoursPlayed { get; set; } = 0;

        [BindProperty] 
        public MediaTrackingFormat Format { get; set; } = MediaTrackingFormat.Digital;

        [BindProperty] 
        public MediaTrackingStatus Status { get; set; } = MediaTrackingStatus.InProgress;

        [BindProperty] 
        public MediaTrackingOwnership Ownership { get; set; } = MediaTrackingOwnership.Owned;

        public EditModel(IMediator mediator)
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

            try
            {
                var gameResult = await _mediator.Send(new GetGameQuery()
                {
                    GameId = GameId
                });

                Game = gameResult;

                var gameTrackingResult = await _mediator.Send(new GetGameTrackingQuery()
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = GameId,
                    Platform = Platform
                });

                GameTracking = gameTrackingResult;

                HoursPlayed = gameTrackingResult.HoursPlayed;
                Platform = gameTrackingResult.Platform;
                Status = gameTrackingResult.Status;
                Format = gameTrackingResult.Format;
                Ownership = gameTrackingResult.Ownership;

                return Page();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            await _mediator.Send(new UpdateGameTrackingCommand
            {
                UserId = Guid.Parse(userIdClaim.Value),
                GameId = GameId,
                HoursPlayed = HoursPlayed,
                Platform = Platform,
                Format = Format,
                Status = Status,
                Ownership = Ownership
            });

            TempData["notifySuccess"] = "Successfully edited game tracking.";

            return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? $"/Home/Games/Id/{GameId}" : ReturnUrl);
        }

        public async Task<IActionResult> OnPostRemoveAsync(string? returnUrl = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            await _mediator.Send(new RemoveGameTrackingCommand()
            {
                UserId = Guid.Parse(userIdClaim.Value),
                GameId = GameId,
                Platform = Platform,
            });

            TempData["notifySuccess"] = "Successfully removed game from tracking.";
            
            return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? $"/Home/Games/Id/{GameId}" : ReturnUrl);
        }
    }
}
