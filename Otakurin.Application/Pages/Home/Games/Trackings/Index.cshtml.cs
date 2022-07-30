using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Common;
using Otakurin.Core.Games.Tracking;

namespace Otakurin.Application.Pages.Home.Games.Trackings
{
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;

        [BindProperty]
        public PagedListResult<GetAllGameTrackingsItemResult> PagedGameTrackings { get; private set; }

        public IndexModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> OnGetAsync(int pageNo = 1)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {

                return Unauthorized();
            }

            var pagedGameTrackingsResult = await _mediator.Send(new GetAllGameTrackingsQuery
            {
                Page = pageNo,
                PageSize = 5,
                UserId = Guid.Parse(userIdClaim.Value),
                Status = null,
                SortByRecentlyModified = true,
                SortByHoursPlayed = false,
                SortByPlatform = false,
                SortByFormat = false,
                SortByOwnership = false
            });
            PagedGameTrackings = pagedGameTrackingsResult;

            return Page();
        }
    }
}
