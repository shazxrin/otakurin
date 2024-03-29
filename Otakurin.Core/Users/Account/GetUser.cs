﻿using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Users.Account;

public class GetUserQuery : IRequest<GetUserResult>
{
    public Guid UserId { get; set; } = Guid.Empty;

}

public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetUserResult
{
    public string UserName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string ProfilePictureURL { get; set; } = string.Empty;
    
    public string Bio { get; set; } = string.Empty;
}

public class GetUserHandler : IRequestHandler<GetUserQuery, GetUserResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetUserHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<GetUserResult> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetUserValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var user = await _databaseContext.Users
            .Where(u => u.Id.Equals(query.UserId))
            .Join(
                _databaseContext.UserProfiles, 
                u => u.Id, 
                p => p.UserId,
                (u, p) => new GetUserResult
                {
                    UserName = u.UserName ?? string.Empty, 
                    Email = u.Email ?? string.Empty, 
                    ProfilePictureURL = p.ProfilePictureURL, 
                    Bio = p.Bio
                }
            )
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found!");
        }
    
        return _mapper.Map<GetUserResult>(user);
    }
}