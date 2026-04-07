using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.Common.Authentication;

public interface ICurrentUser
{
    UserId UserId { get; }
}
