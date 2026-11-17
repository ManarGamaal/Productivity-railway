namespace TaskFlow.Application.Common.Models;

public sealed record UserIdentity(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);
