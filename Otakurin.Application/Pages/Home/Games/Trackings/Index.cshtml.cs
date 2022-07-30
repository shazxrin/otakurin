using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Common;
using Otakurin.Core.Games.Tracking;
using Otakurin.Domain.Tracking;

namespace Otakurin.Application.Pages.Home.Games.Trackings
{
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = MediaTrackingStatus.InProgress.ToString();

        [BindProperty(SupportsGet = true)] 
        public int PageNo { get; set; } = 1;
        
        [BindProperty]
        public PagedListResult<GetAllGameTrackingsItemResult> PagedGameTrackings{ get; private set; }

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

            var pagedGameTrackingsResult = await _mediator.Send(new GetAllGameTrackingsQuery
            {
                Page = PageNo,
                PageSize = 5,
                UserId = Guid.Parse(userIdClaim.Value),
                Status = Enum.Parse<MediaTrackingStatus>(Status),
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
