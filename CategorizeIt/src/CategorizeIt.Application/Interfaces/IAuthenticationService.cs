using CategorizeIt.Application.Models.Authentication;

namespace CategorizeIt.Application.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);
    Task<AuthenticationResponse> LoginAsync(LoginRequest request);
    Task<AuthenticationResponse> GoogleLoginAsync(GoogleLoginRequest request);
}