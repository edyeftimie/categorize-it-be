using CategorizeIt.Application.Models.Authentication;

namespace CategorizeIt.Application.Interfaces;

public interface IGoogleAuthenticationProvider
{
    Task<GoogleUserModel> ValidateTokenAsync(string idToken);
}