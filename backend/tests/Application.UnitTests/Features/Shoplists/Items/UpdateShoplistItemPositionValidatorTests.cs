using FluentValidation.TestHelper;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.UnitTests.Features.Shoplists.Items;

public sealed class UpdateShoplistItemPositionValidatorTests
{
    private readonly UpdateShoplistItemPosition.Validator _sut = new();

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(1000)]
    public async Task ValidRequest_NoErrors(int position)
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(ShoplistId.New(), ShoplistItemId.New(), position)
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShoplistId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(ShoplistId: null, ShoplistItemId.New(), 1)
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task ItemId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(ShoplistId.New(), ItemId: null, 1)
        );

        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task Position_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(
                ShoplistId.New(),
                ShoplistItemId.New(),
                Position: null
            )
        );

        result.ShouldHaveValidationErrorFor(r => r.Position).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    [NegativeIntegers(includeZero: true)]
    public async Task Position_LessThanOne_ProducesInvalidError(int position)
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(ShoplistId.New(), ShoplistItemId.New(), position)
        );

        result.ShouldHaveValidationErrorFor(r => r.Position).WithErrorMessage(ErrorCodes.Invalid);
    }

    [Test]
    public async Task Position_Null_OnlyEmitsRequiredNotInvalid()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(
                ShoplistId.New(),
                ShoplistItemId.New(),
                Position: null
            )
        );

        // RuleLevelCascadeMode = Stop on BaseValidator: NotNull stops the chain,
        // GreaterThanOrEqualTo never runs, so only the Required message is emitted.
        var positionErrors = result
            .Errors.Where(e =>
                string.Equals(
                    e.PropertyName,
                    nameof(UpdateShoplistItemPosition.Request.Position),
                    StringComparison.Ordinal
                )
            )
            .ToList();

        await Assert.That(positionErrors.Count).IsEqualTo(1);
        await Assert.That(positionErrors[0].ErrorMessage).IsEqualTo(ErrorCodes.Required);
    }

    [Test]
    public async Task AllFieldsNull_ProducesThreeErrors()
    {
        var result = await _sut.TestValidateAsync(
            new UpdateShoplistItemPosition.Request(ShoplistId: null, ItemId: null, Position: null)
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.Position).WithErrorMessage(ErrorCodes.Required);
    }
}
