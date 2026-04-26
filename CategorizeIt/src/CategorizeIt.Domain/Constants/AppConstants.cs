namespace CategorizeIt.Domain.Constants;

public static class AppConstants
{
    public static class Jwt
    {
        public const int DefaultExpirationInMinutes = 60;
        public const int RefreshTokenExpirationInDays = 7;
    }

    public static class Validation
    {
        public const int EmailMaxLength = 256;
        public const int NameMaxLength = 100;
        public const int PasswordMinLength = 8;
    }
}