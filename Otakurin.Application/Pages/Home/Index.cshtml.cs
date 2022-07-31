using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Common;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Tracking;

namespace Otakurin.Application.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;
        public PagedListResult<GetAllGameTrackingsItemResult> RecentPagedGameTrackings { get; private set; }

        public IndexModel(IMediator mediator)
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

            var pagedGameTrackingsResult = await _mediator.Send(new GetAllGameTrackingsQuery
            {
                Page = 1,
                PageSize = 4,
                UserId = Guid.Parse(userIdClaim.Value),
                Status = null,
                SortByRecentlyModified = true,
                SortByHoursPlayed = false,
                SortByPlatform = false,
                SortByFormat = false,
                SortByOwnership = false
            });

            RecentPagedGameTrackings = pagedGameTrackingsResult;

            return Page();
        }
    }
}
