using FluentValidation.TestHelper;
using Shoplists.Application.Common.Constants;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.UnitTests.Features.Shoplists.Items;

public sealed class UpdateShoplistItemValidatorTests
{
    private readonly UpdateShoplistItem.Validator _sut = new();

    [Test]
    public async Task ValidRequest_NoErrors()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItem.Request(ShoplistId.New(), ShoplistItemId.New(), "Milk")
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShoplistId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItem.Request(ShoplistId: null, ShoplistItemId.New(), "Milk")
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task ItemId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItem.Request(ShoplistId.New(), ItemId: null, "Milk")
        );

        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task Name_NullEmptyOrWhitespace_ProducesRequiredError(string? name)
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItem.Request(ShoplistId.New(), ShoplistItemId.New(), name)
        );

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task Name_OverMaxLength_ProducesTooLongError()
    {
        var overMax = new string('a', ApplicationConstants.DefaultMaxStringLength + 1);

        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItem.Request(ShoplistId.New(), ShoplistItemId.New(), overMax)
        );

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.TooLong);
    }

    [Test]
    public async Task AllFieldsNull_ProducesAllThreeErrors()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItem.Request(ShoplistId: null, ItemId: null, Name: null)
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.Required);
    }
}
