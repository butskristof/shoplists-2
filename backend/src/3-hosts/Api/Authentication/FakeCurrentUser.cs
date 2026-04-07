using Shoplists.Application.Common.Authentication;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Api.Authentication;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public UserId UserId { get; } = new("FAKE_USER");
}
