using FluentValidation.TestHelper;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.UnitTests.Features.Shoplists;

public sealed class GetShoplistValidatorTests
{
    private readonly GetShoplist.Validator _sut = new();

    [Test]
    public async Task ValidRequest_NoErrors()
    {
        var result = await _sut.TestValidateAsync(new GetShoplist.Request(ShoplistId.New()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Id_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(new GetShoplist.Request(Id: null));

        result.ShouldHaveValidationErrorFor(r => r.Id).WithErrorMessage(ErrorCodes.Required);
    }
}
