using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Domain.Common;

namespace RestaurantSystem.Api.Features.Auth.Commands.LoginCommand;

public record LoginCommand(string Email, string Password) : ICommand<LoginResponseDto>;

public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponseDto>
{

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
  
    public LoginCommandHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        //var user = await _userManager.FindByEmailAsync(command.Email);

        //if (user == null || !await _userManager.CheckPasswordAsync(user, command.Password))
        //{
        //    throw new UnauthorizedAccessException("Invalid email or password");
        //}


        //var roles = await _userManager.GetRolesAsync(user);
        //var claims = new List<Claim>
        //{
        //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
        //    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        //    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //};

        //foreach (var role in roles)
        //{
        //    claims.Add(new Claim(ClaimTypes.Role, role));
        //}

        //var accessToken = GenerateAccessToken(claims);
        //var refreshToken = GenerateRefreshToken();

        //user.RefreshToken = refreshToken;
        //user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        //await _userManager.UpdateAsync(user);

        //return new LoginResponseDto
        //{
        //    AccessToken = accessToken,
        //    RefreshToken = refreshToken,
        //    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60")),
        //    User = new UserDto
        //    {
        //        Id = user.Id,
        //        Email = user.Email ?? string.Empty,
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        Roles = roles.ToList()
        //    }
        //};

        throw new NotImplementedException();
    }

    //private string GenerateAccessToken(List<Claim> claims)
    //{
    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"] ?? "your_default_secret_key_here_min_16chars"));
    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    //    var expiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60"));

    //    var token = new JwtSecurityToken(
    //        issuer: _configuration["JwtSettings:Issuer"],
    //        audience: _configuration["JwtSettings:Audience"],
    //        claims: claims,
    //        expires: expiry,
    //        signingCredentials: creds
    //    );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}

    //private string GenerateRefreshToken()
    //{
    //    var randomNumber = new byte[32];
    //    using var rng = RandomNumberGenerator.Create();
    //    rng.GetBytes(randomNumber);
    //    return Convert.ToBase64String(randomNumber);
    //}
}
