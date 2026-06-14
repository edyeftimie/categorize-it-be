using Microsoft.Extensions.Options;
using Google.Apis.Auth;
using CategorizeIt.Application.Models.Authentication;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Settings;

namespace CategorizeIt.Infrastructure.ExternalClients;

public class GoogleAuthenticationProvider : IGoogleAuthenticationProvider
{
    private readonly GoogleAuthenticationSettings _googleAuthSettings;

    public GoogleAuthenticationProvider(IOptions<GoogleAuthenticationSettings> googleAuthSettings)
    {
        _googleAuthSettings = googleAuthSettings.Value;
    }

    public async Task<GoogleUserModel> ValidateTokenAsync(string idToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleAuthSettings.ClientId }
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Invalid Google token.", ex);
        }

        return new GoogleUserModel
        {
            GoogleId = payload.Subject,
            Email = payload.Email,
            Name = payload.Name
        };
    }
}