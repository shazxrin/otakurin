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

public class FetchBookCommand : IRequest<FetchBookResult>
{
    public string BookRemoteId { get; set; } = string.Empty;
}

public class FetchBookValidator : AbstractValidator<FetchBookCommand>
{
    public FetchBookValidator()
    {
        RuleFor(c => c.BookRemoteId).NotEmpty();
    }
}

public class FetchBookResult
{
    public Guid BookId { get; set; }
}

public class FetchBookMappings : Profile
{
    public FetchBookMappings()
    {
        CreateMap<APIBook, Book>()
            .ForMember(book => book.Id,
                options => options.Ignore())
            .ForMember(
                book => book.RemoteId,
                options => options.MapFrom(apiBook => apiBook.Id))
            .ForMember(
                book => book.AuthorsString,
                options => options.MapFrom(apiBook => string.Join(";", apiBook.Authors)));
    }
}

public class FetchBookHandler : IRequestHandler<FetchBookCommand, FetchBookResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IBookService _bookService;
    private readonly IMapper _mapper;

    public FetchBookHandler(DatabaseContext databaseContext, IBookService bookService, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _bookService = bookService;
        _mapper = mapper;
    }
    
    public async Task<FetchBookResult> Handle(FetchBookCommand command, CancellationToken cancellationToken)
    {
        var validator = new FetchBookValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }

        var dbBook = await _databaseContext.Books.AsNoTracking()
            .FirstOrDefaultAsync(b => b.RemoteId.Equals(command.BookRemoteId), cancellationToken);

        if (dbBook != null)
        {
            return new FetchBookResult { BookId = dbBook.Id};
        }

        var apiBook = await _bookService.GetBookById(command.BookRemoteId);

        if (apiBook == null)
        {
            throw new NotFoundException("API Book not found");
        }

        var newDBBook = _mapper.Map<APIBook, Book>(apiBook);
        _databaseContext.Books.Add(newDBBook);
        await _databaseContext.SaveChangesAsync(cancellationToken);

        return new FetchBookResult { BookId = newDBBook.Id };
    }
}