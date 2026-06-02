using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shoplists.Api.Authentication;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Api.UnitTests.Authentication;

public sealed class HttpContextCurrentUserTests
{
    private sealed class StubHttpContextAccessor(HttpContext? httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext
        {
            get => httpContext;
            set => throw new NotSupportedException();
        }
    }

    private static HttpContextCurrentUser CreateSut(HttpContext? httpContext) =>
        new(new StubHttpContextAccessor(httpContext));

    private static DefaultHttpContext HttpContextWithClaims(params Claim[] claims) =>
        new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test")),
        };

    [Test]
    public async Task UserId_SubClaimPresent_ReturnsUserIdFromClaim()
    {
        var sut = CreateSut(HttpContextWithClaims(new Claim("sub", "user-123")));

        var userId = sut.UserId;

        await Assert.That(userId).IsEqualTo(new UserId("user-123"));
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task UserId_SubClaimNullOrWhitespace_ThrowsInvalidOperationException(
        string? subValue
    )
    {
        Claim[] claims = subValue is null ? [] : [new Claim("sub", subValue)];
        var sut = CreateSut(HttpContextWithClaims(claims));

        await Assert.That(() => sut.UserId).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task UserId_NoHttpContext_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(httpContext: null);

        await Assert.That(() => sut.UserId).Throws<InvalidOperationException>();
    }
}
