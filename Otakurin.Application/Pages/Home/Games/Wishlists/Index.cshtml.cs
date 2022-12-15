using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Common;
using Otakurin.Core.Games.Tracking;
using Otakurin.Core.Games.Wishlist;
using Otakurin.Domain.Tracking;

namespace Otakurin.Application.Pages.Home.Games.Wishlists
{
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;
        
        [BindProperty(SupportsGet = true)] 
        public int PageNo { get; set; } = 1;

        public PagedListResult<GetAllGameWishlistsItemResult> PagedGameWishlists { get; private set; } 
            = new(new (), 0, 1, 1);

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

            var pagedGameWishlistsResult = await _mediator.Send(new GetAllGameWishlistsQuery()
            {
                Page = PageNo,
                PageSize = 10,
                UserId = Guid.Parse(userIdClaim.Value),
                SortByRecentlyModified = true,
                SortByPlatform = false,
            });

            PagedGameWishlists = pagedGameWishlistsResult;

            return Page();
        }
    }
}
