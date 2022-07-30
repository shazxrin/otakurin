using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otakurin.Core.Games.Content;

namespace Otakurin.Application.Pages.Home.Games
{
    public class SearchModel : PageModel
    {
        private readonly IMediator _mediator;

        [FromQuery(Name = "title")] 
        public string Title { get; set; } = string.Empty;

        public IEnumerable<SearchGamesResult.SearchGamesItemResult> SearchGamesItems { get; set; } 
            = Enumerable.Empty<SearchGamesResult.SearchGamesItemResult>();

        public SearchModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task OnGet()
        {
            if (!string.IsNullOrEmpty(Title))
            {
                var result = await _mediator.Send(new SearchGamesQuery()
                {
                    Title = Title
                });

                SearchGamesItems = result.Items;
            }
        }

        public async Task<IActionResult> OnPostFetchAsync(long id)
        {
            var result = await _mediator.Send(new FetchGameCommand()
            {
                GameRemoteId = id
            });

            return LocalRedirect($"/Home/Games/Id/{result.GameId}");
        }
    }
}
