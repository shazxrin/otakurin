using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Otakurin.Domain.User;

namespace Otakurin.Core.Services;

public class TokenService
{
    private readonly SigningCredentials _signingCredentials;
    private readonly SymmetricSecurityKey _symmetricSecurityKey;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(string secretKey)
    {
        _symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _signingCredentials = new SigningCredentials(_symmetricSecurityKey, SecurityAlgorithms.HmacSha512Signature);
        _tokenHandler = new JwtSecurityTokenHandler();
    }
    
    public string CreateToken(UserAccount userAccount)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userAccount.Id.ToString()),
            new Claim(ClaimTypes.Name, userAccount.UserName),
            new Claim(ClaimTypes.Email, userAccount.Email),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(1),
            IssuedAt = DateTime.Now,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);

        return _tokenHandler.WriteToken(token);
    }
}