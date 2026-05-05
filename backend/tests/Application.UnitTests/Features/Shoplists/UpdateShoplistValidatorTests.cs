using FluentValidation.TestHelper;
using Shoplists.Application.Common.Constants;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.UnitTests.Features.Shoplists;

public sealed class UpdateShoplistValidatorTests
{
    private readonly UpdateShoplist.Validator _sut = new();

    [Test]
    public async Task ValidRequest_NoErrors()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplist.Request(ShoplistId.New(), "Groceries")
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Id_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplist.Request(Id: null, "Groceries")
        );

        result.ShouldHaveValidationErrorFor(r => r.Id).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task Name_NullEmptyOrWhitespace_ProducesRequiredError(string? name)
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplist.Request(ShoplistId.New(), name)
        );

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task Name_OverMaxLength_ProducesTooLongError()
    {
        var overMax = new string('a', ApplicationConstants.DefaultMaxStringLength + 1);

        var result = await _sut.TestValidateAsync(
            new UpdateShoplist.Request(ShoplistId.New(), overMax)
        );

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.TooLong);
    }

    [Test]
    public async Task IdAndName_BothInvalid_ProducesBothErrors()
    {
        var result = await _sut.TestValidateAsync(new UpdateShoplist.Request(Id: null, Name: null));

        result.ShouldHaveValidationErrorFor(r => r.Id).WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.Required);
    }
}
