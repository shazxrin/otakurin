using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Exceptions;
using Otakurin.Core.Games.Content;
using Otakurin.Core.Games.Tracking;
using Otakurin.Core.Games.Wishlist;
using Otakurin.Domain.Tracking;

namespace Otakurin.Application.Pages.Home.Games.Wishlists
{
    public class AddModel : PageModel
    {
        private readonly IMediator _mediator;

        [BindProperty(SupportsGet = true)]
        public Guid GameId { get; set; }

        public GetGameResult Game { get; private set; }

        public GetGameWishlistsResult GameWishlists { get; private set; }

        [BindProperty]
        public string Platform { get; set; } = string.Empty;

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

                var gameWishlistsResult = await _mediator.Send(new GetGameWishlistsQuery()
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = GameId
                });

                GameWishlists = gameWishlistsResult;

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

            await _mediator.Send(new AddGameWishlistCommand()
            {
                UserId = Guid.Parse(userIdClaim.Value),
                GameId = GameId,
                Platform = Platform,
            });

            TempData["notifySuccess"] = "Successfully added game to wishlist.";
            
            return LocalRedirect($"/Home/Games/Id/{GameId}");
        }
    }
}
