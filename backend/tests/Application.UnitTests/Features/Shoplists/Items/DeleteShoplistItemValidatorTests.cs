using FluentValidation.TestHelper;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.UnitTests.Features.Shoplists.Items;

public sealed class DeleteShoplistItemValidatorTests
{
    private readonly DeleteShoplistItem.Validator _sut = new();

    [Test]
    public async Task ValidRequest_NoErrors()
    {
        var result = await _sut.TestValidateAsync(
            new DeleteShoplistItem.Request(ShoplistId.New(), ShoplistItemId.New())
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShoplistId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new DeleteShoplistItem.Request(ShoplistId: null, ShoplistItemId.New())
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task ItemId_Null_ProducesRequiredError()
    {
        var result = await _sut.TestValidateAsync(
            new DeleteShoplistItem.Request(ShoplistId.New(), ItemId: null)
        );

        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task BothFieldsNull_ProducesBothErrors()
    {
        var result = await _sut.TestValidateAsync(
            new DeleteShoplistItem.Request(ShoplistId: null, ItemId: null)
        );

        result
            .ShouldHaveValidationErrorFor(r => r.ShoplistId)
            .WithErrorMessage(ErrorCodes.Required);
        result.ShouldHaveValidationErrorFor(r => r.ItemId).WithErrorMessage(ErrorCodes.Required);
    }
}
