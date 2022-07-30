using AutoMapper;
using Otakurin.Domain.Wishlist;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Wishlist;

public class AddBookWishlistCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
}

public class AddBookWishlistValidator : AbstractValidator<AddBookWishlistCommand>
{
    public AddBookWishlistValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.BookId).NotEmpty();
    }
}

public class AddBookWishlistMappings : Profile
{
    public AddBookWishlistMappings()
    {
        CreateMap<AddBookWishlistCommand, BookWishlist>();
    }
}

public class AddBookWishlistHandler : IRequestHandler<AddBookWishlistCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public AddBookWishlistHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(AddBookWishlistCommand command, CancellationToken cancellationToken)
    {
        var validator = new AddBookWishlistValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        // Verify user.
        bool isUserExists = await _databaseContext.Users
            .AsNoTracking()
            .Where(u => u.Id.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (!isUserExists)
        {
            throw new NotFoundException("User not found!");
        }
        
        // Verify if tracked book already exist.
        bool isBookWishlistExists = await _databaseContext.BookWishlists
            .AsNoTracking()
            .Where(bw => bw.BookId.Equals(command.BookId) 
                         && bw.UserId.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (isBookWishlistExists)
        {
            throw new ExistsException("Book wishlist already exists!");
        }
        
        // Verify book id.
        bool isBookExists = await _databaseContext.Books
            .AsNoTracking()
            .Where(b => b.Id.Equals(command.BookId))
            .AnyAsync(cancellationToken);
        if (!isBookExists)
        {
            throw new NotFoundException("Book not found!");
        }

        var bookWishlist = _mapper.Map<AddBookWishlistCommand, BookWishlist>(command);
        _databaseContext.BookWishlists.Add(bookWishlist);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
