namespace TaskFlow.Application.Common;

/// <summary>Claim names used inside the JWT access token.</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string GivenName = "given_name";
    public const string FamilyName = "family_name";
    public const string Role = "role";
}
