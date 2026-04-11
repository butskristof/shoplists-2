using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.Common.Authentication;

/// <summary>
/// Represents the current user in the application's context.
/// This interface is used to provide access to user-specific information
/// within the application's various layers (e.g., authentication, persistence).
/// </summary>
public interface ICurrentUser
{
    UserId UserId { get; }
}
