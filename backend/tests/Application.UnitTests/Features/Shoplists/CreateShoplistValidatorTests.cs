using FluentValidation.TestHelper;
using Shoplists.Application.Common.Constants;
using Shoplists.Application.Common.Validation;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.UnitTests.Features.Shoplists;

public sealed class CreateShoplistValidatorTests
{
    private readonly CreateShoplist.Validator _sut = new();

    [Test]
    public async Task ValidRequest_NoErrors()
    {
        var result = await _sut.TestValidateAsync(new CreateShoplist.Request("Groceries"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task Name_NullEmptyOrWhitespace_ProducesRequiredError(string? name)
    {
        var result = await _sut.TestValidateAsync(new CreateShoplist.Request(name));

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task Name_AtMaxLength_NoErrors()
    {
        var atMax = new string('a', ApplicationConstants.DefaultMaxStringLength);

        var result = await _sut.TestValidateAsync(new CreateShoplist.Request(atMax));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Name_OverMaxLength_ProducesTooLongError()
    {
        var overMax = new string('a', ApplicationConstants.DefaultMaxStringLength + 1);

        var result = await _sut.TestValidateAsync(new CreateShoplist.Request(overMax));

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage(ErrorCodes.TooLong);
    }
}
