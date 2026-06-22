using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Settings;
using CategorizeIt.Application.Models.Authentication;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Domain.Enums;

namespace CategorizeIt.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly IGoogleAuthenticationProvider _googleAuthProvider;

    public AuthenticationService(IUserRepository userRepository, IOptions<JwtSettings> jwtSettings, IGoogleAuthenticationProvider googleAuthProvider)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _googleAuthProvider = googleAuthProvider;
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);

        if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new Exception("Invalid email or password.");
        }

        return GenerateAuthenticationResponse(user);
    }

    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);

        if (existingUser != null)
        {
            throw new Exception("User with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Username = request.Username
        };

        await _userRepository.CreateUserAsync(user);

        return GenerateAuthenticationResponse(user);
    }

    public async Task<AuthenticationResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        GoogleUserModel googleUser;
        try
        {
            googleUser = await _googleAuthProvider.ValidateTokenAsync(request.IdToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Invalid Google ID token.", ex);
        }

        var user = await _userRepository.GetUserByEmailAsync(googleUser.Email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = googleUser.Email,
                Username = googleUser.Name ?? googleUser.Email.Split('@')[0],
                GoogleId = googleUser.GoogleId,
                Role = Role.User
            };

            await _userRepository.CreateUserAsync(user);
        } 
        else if (user.GoogleId == null)
        {
            user.GoogleId = googleUser.GoogleId;
            await _userRepository.UpdateUserAsync(user);
        }

        return GenerateAuthenticationResponse(user);
    }

    private AuthenticationResponse GenerateAuthenticationResponse(User user)
    {
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new AuthenticationResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username
            }
        };
    }
}