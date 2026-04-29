using FluentValidation.TestHelper;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.UnitTests.Features.Shoplists.Items;

public sealed class UpdateShoplistItemFulfilledValidatorTests
{
    private readonly UpdateShoplistItemFulfilled.Validator _sut = new();

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ValidRequest_NoErrors(bool isFulfilled)
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemFulfilled.Request(
                ShoplistId.New(),
                ShoplistItemId.New(),
                isFulfilled
            )
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShoplistId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemFulfilled.Request(
                ShoplistId: null,
                ShoplistItemId.New(),
                IsFulfilled: true
            )
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task ItemId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemFulfilled.Request(
                ShoplistId.New(),
                ItemId: null,
                IsFulfilled: true
            )
        );

        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task IsFulfilled_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemFulfilled.Request(
                ShoplistId.New(),
                ShoplistItemId.New(),
                IsFulfilled: null
            )
        );

        result
            .ShouldHaveValidationErrorFor(r => r.IsFulfilled)
            .WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task AllFieldsNull_ProducesAllThreeErrors()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemFulfilled.Request(
                ShoplistId: null,
                ItemId: null,
                IsFulfilled: null
            )
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
        result
            .ShouldHaveValidationErrorFor(r => r.IsFulfilled)
            .WithErrorMessage(ErrorCodes.Required);
    }
}
