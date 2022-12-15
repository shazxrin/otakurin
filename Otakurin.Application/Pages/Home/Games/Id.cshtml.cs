using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Content;
using Otakurin.Core.Games.Tracking;
using Otakurin.Core.Games.Wishlist;

namespace Otakurin.Application.Pages.Home.Games
{
    public class IdModel : PageModel
    {
        private readonly IMediator _mediator;

        [BindProperty(SupportsGet = true)]
        public Guid GameId { get; set; } = Guid.Empty;

        [BindProperty] 
        public GetGameResult Game { get; private set; } = new();

        [BindProperty] 
        public GetGameTrackingsResult GameTrackings { get; private set; } = new();

        [BindProperty] 
        public GetGameWishlistsResult GameWishlists { get; private set; } = new();

        public IdModel(IMediator mediator)
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

                var trackingsResult = await _mediator.Send(new GetGameTrackingsQuery()
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = GameId
                });
                GameTrackings = trackingsResult;

                var wishlistsResult = await _mediator.Send(new GetGameWishlistsQuery()
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = GameId
                });
                GameWishlists = wishlistsResult;

                return Page();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}
