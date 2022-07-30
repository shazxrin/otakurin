using AutoMapper;
using FluentValidation;
using MediatR;
using Otakurin.Service.Book;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Content;

public class SearchBooksQuery : IRequest<SearchBooksResult>
{
    public string Title { get; set; }
}

public class SearchBooksValidator : AbstractValidator<SearchBooksQuery>
{
    public SearchBooksValidator()
    {
        RuleFor(q => q.Title).NotEmpty();
    }
}

public class SearchBooksResult
{
    public class SearchBooksItemResult
    {
        public string RemoteId { get; set; }
        
        public string Title { get; set; }
        
        public string CoverImageURL { get; set; }
        
        public List<string> Authors { get; set; }
    }
    
    public List<SearchBooksItemResult> Items { get; set; }
}

public class SearchBooksMappings : Profile
{
    public SearchBooksMappings()
    {
        CreateMap<APIBookBasic, SearchBooksResult.SearchBooksItemResult>()
            .ForMember(
                sir => sir.RemoteId,
                options => options.MapFrom(apiBook => apiBook.Id));
    }
}

public class SearchBooksHandler : IRequestHandler<SearchBooksQuery, SearchBooksResult>
{
    private readonly IBookService _bookService;
    private readonly IMapper _mapper;

    public SearchBooksHandler(IBookService bookService, IMapper mapper)
    {
        _bookService = bookService;
        _mapper = mapper;
    }
    
    public async Task<SearchBooksResult> Handle(SearchBooksQuery query, CancellationToken cancellationToken)
    {
        var validator = new SearchBooksValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var books = await _bookService.SearchBookByTitle(query.Title);

        return new SearchBooksResult {
            Items = books.Select(_mapper.Map<APIBookBasic, SearchBooksResult.SearchBooksItemResult>).ToList()
        };
    }
}
