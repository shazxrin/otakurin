using AutoMapper;
using FluentValidation;
using MediatR;
using Otakurin.Service.Game;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Content;

public class SearchGamesQuery : IRequest<SearchGamesResult>
{
    public string Title { get; set; }
}

public class SearchGamesValidator : AbstractValidator<SearchGamesQuery>
{
    public SearchGamesValidator()
    {
        RuleFor(q => q.Title).NotEmpty();
    }
}

public class SearchGamesResult
{
    public class SearchGamesItemResult
    {
        public long RemoteId { get; set; }
        
        public string Title { get; set; }
        
        public string CoverImageURL { get; set; }
        
        public List<string> Platforms { get; set; }
    }

    public List<SearchGamesItemResult> Items { get; set; }
}

public class SearchGamesMappings : Profile
{
    public SearchGamesMappings()
    {
        CreateMap<APIGameBasic, SearchGamesResult.SearchGamesItemResult>()
            .ForMember(
                sgir => sgir.RemoteId,
                options => options.MapFrom(apiGame => apiGame.Id));
    }
}

public class SearchGamesHandler : IRequestHandler<SearchGamesQuery, SearchGamesResult>
{
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public SearchGamesHandler(IGameService gameService, IMapper mapper)
    {
        _gameService = gameService;
        _mapper = mapper;
    }
    
    public async Task<SearchGamesResult> Handle(SearchGamesQuery query, CancellationToken cancellationToken)
    {
        var validator = new SearchGamesValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var games = await _gameService.SearchGameByTitle(query.Title);

        return new SearchGamesResult {
            Items = games.Select(_mapper.Map<APIGameBasic, SearchGamesResult.SearchGamesItemResult>).ToList()
        };
    }
}
