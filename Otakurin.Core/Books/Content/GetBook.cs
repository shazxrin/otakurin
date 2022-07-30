using AutoMapper;
using Otakurin.Domain.Media;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using Otakurin.Service.Book;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Content;

public class GetBookQuery : IRequest<GetBookResult>
{
    public Guid BookId { get; set; }
}

public class GetBookValidator : AbstractValidator<GetBookQuery>
{
    public GetBookValidator()
    {
        RuleFor(q => q.BookId).NotEmpty();
    }
}

public class GetBookResult
{
    public Guid Id { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public string Title { get; set; }
    
    public string Summary { get; set; }
    
    public List<string> Authors { get; set; }
}

public class GetBookMappings : Profile
{
    public GetBookMappings()
    {
        CreateMap<Book, GetBookResult>()
            .ForMember(
                gbr => gbr.Authors,
                options => options.MapFrom(book => book.AuthorsString.Split(';', StringSplitOptions.None)));
    }
}

public class GetBookHandler : IRequestHandler<GetBookQuery, GetBookResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IBookService _bookService;
    private readonly IMapper _mapper;

    public GetBookHandler(DatabaseContext databaseContext, IBookService bookService, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _bookService = bookService;
        _mapper = mapper;
    }

    public async Task<GetBookResult> Handle(GetBookQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetBookValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        // Find book from database.
        var dbBook = await _databaseContext.Books
            .Where(book => book.Id.Equals(query.BookId))
            .FirstOrDefaultAsync(cancellationToken);
        if (dbBook == null)
        {
            throw new NotFoundException("Book not found!");
        }
        
        // Refresh book from remote if cache is stale.
        var timeSpan = DateTime.Now - dbBook.LastModifiedOn;
        if (timeSpan?.TotalHours > 12)
        {
            var remoteBook = await _bookService.GetBookById(dbBook.RemoteId);
            if (remoteBook != null)
            {
                _mapper.Map<APIBook, Book>(remoteBook, dbBook);
                _databaseContext.Books.Update(dbBook);
                await _databaseContext.SaveChangesAsync(cancellationToken);

                return _mapper.Map<Book, GetBookResult>(dbBook);
            }
        }

        return _mapper.Map<Book, GetBookResult>(dbBook);
    }
}