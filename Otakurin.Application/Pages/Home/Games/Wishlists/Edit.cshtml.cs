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
    public class EditModel : PageModel
    {
        private readonly IMediator _mediator;
        
        public GetGameResult Game { get; private set; }

        public GetGameWishlistResult GameWishlistResult { get; private set; }

        [BindProperty(SupportsGet = true)] 
        public Guid GameId { get; set; } = Guid.Empty;
        
        [BindProperty(SupportsGet = true)] 
        public string Platform { get; set; } = string.Empty;
        
        [BindProperty(SupportsGet = true)] 
        public string ReturnUrl { get; set; } = string.Empty;

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

                var gameTrackingResult = await _mediator.Send(new GetGameWishlistQuery()
                {
                    UserId = Guid.Parse(userIdClaim.Value),
                    GameId = GameId,
                    Platform = Platform
                });

                GameWishlistResult = gameTrackingResult;

                return Page();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostRemoveAsync(string? returnUrl = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            await _mediator.Send(new RemoveGameWishlistCommand()
            {
                UserId = Guid.Parse(userIdClaim.Value),
                GameId = GameId,
                Platform = Platform,
            });

            TempData["notifySuccess"] = "Successfully removed game from wishlist.";
            
            return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? $"/Home/Games/Id/{GameId}" : ReturnUrl);
        }
    }
}
