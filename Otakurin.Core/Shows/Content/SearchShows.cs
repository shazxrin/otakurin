using AutoMapper;
using Otakurin.Domain.Media;
using FluentValidation;
using MediatR;
using Otakurin.Service.Show;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Shows.Content;

public class SearchShowsQuery : IRequest<SearchShowsResult>
{
    public string Title { get; set; }
}

public class SearchShowsValidator : AbstractValidator<SearchShowsQuery>
{
    public SearchShowsValidator()
    {
        RuleFor(q => q.Title).NotEmpty();
    }
}

public class SearchShowsResult
{
    public class SearchShowsItemResult
    {
        public string RemoteId { get; set; }
        
        public string Title { get; set; }
        
        public string CoverImageURL { get; set; }
        
        public ShowType ShowType { get; set; }
    }

    public List<SearchShowsItemResult> Items { get; set; }
}

public class SearchShowsMappings : Profile
{
    public SearchShowsMappings()
    {
        CreateMap<APIShowBasic, SearchShowsResult.SearchShowsItemResult>()
            .ForMember(
                ssir => ssir.RemoteId,
                options => options.MapFrom(apiShow => apiShow.Id));
    }
}

public class SearchShowsHandler : IRequestHandler<SearchShowsQuery, SearchShowsResult>
{
    private readonly IShowService _showService;
    private readonly IMapper _mapper;
    
    public SearchShowsHandler(IShowService showService, IMapper mapper)
    {
        _showService = showService;
        _mapper = mapper;
    }
    
    public async Task<SearchShowsResult> Handle(SearchShowsQuery query, CancellationToken cancellationToken)
    {
        var validator = new SearchShowsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }

        var shows = await _showService.SearchShowByTitle(query.Title);

        return new SearchShowsResult {
            Items = shows.Select(_mapper.Map<APIShowBasic, SearchShowsResult.SearchShowsItemResult>).ToList()
        };
    }
}