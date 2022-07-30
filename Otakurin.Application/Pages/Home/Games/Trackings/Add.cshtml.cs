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
    public class AddModel : PageModel
    {
        private readonly IMediator _mediator;

        [BindProperty(SupportsGet = true)]
        public Guid GameId { get; set; }

        public GetGameResult Game { get; private set; }

        public GetGameTrackingsResult GameTrackings { get; private set; }

        [BindProperty]
        public int HoursPlayed { get; set; } = 0;

        [BindProperty]
        public string Platform { get; set; } = string.Empty;

        [BindProperty]
        public MediaTrackingFormat Format { get; set; } = MediaTrackingFormat.Digital;

        [BindProperty]
        public MediaTrackingStatus Status { get; set; } = MediaTrackingStatus.InProgress;

        [BindProperty]
        public MediaTrackingOwnership Ownership { get; set; } = MediaTrackingOwnership.Owned;

        public AddModel(IMediator mediator)
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

                var gameTrackingsResult = await _mediator.Send(new GetGameTrackingsQuery()
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = GameId
                });

                GameTrackings = gameTrackingsResult;

                return Page();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {

                return Unauthorized();
            }

            await _mediator.Send(new AddGameTrackingCommand
            {
                UserId = Guid.Parse(userIdClaim.Value),
                GameId = GameId,
                HoursPlayed = HoursPlayed,
                Platform = Platform,
                Format = Format,
                Status = Status,
                Ownership = Ownership
            });

            return LocalRedirect($"/Home/Games/Id/{GameId}");
        }
    }
}
