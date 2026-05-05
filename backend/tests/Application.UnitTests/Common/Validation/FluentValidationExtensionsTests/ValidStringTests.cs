using FluentValidation;
using FluentValidation.TestHelper;
using Shoplists.Application.Common.Constants;
using Shoplists.Application.Common.Validation;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.UnitTests.Common.Validation.FluentValidationExtensionsTests;

public sealed class ValidStringTests
{
    private sealed record Model(string? Value);

    [Test]
    public async Task ValidString_RequiredTrue_ValidValue_NoError()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: true);

        var result = await validator.TestValidateAsync(new Model("anything"));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task ValidString_RequiredTrue_NullEmptyOrWhitespace_ProducesRequiredMessage(
        string? value
    )
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: true);

        var result = await validator.TestValidateAsync(new Model(value));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ValidString_RequiredFalse_NullOrEmpty_NoError(string? value)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: false);

        var result = await validator.TestValidateAsync(new Model(value));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task ValidString_RequiredFalse_ValidValue_NoError()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: false);

        var result = await validator.TestValidateAsync(new Model("anything"));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task ValidString_LengthAtDefaultMax_NoError()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: true);
        var atMax = new string('a', ApplicationConstants.DefaultMaxStringLength);

        var result = await validator.TestValidateAsync(new Model(atMax));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task ValidString_LengthOverDefaultMax_ProducesTooLongMessage()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: true);
        var overMax = new string('a', ApplicationConstants.DefaultMaxStringLength + 1);

        var result = await validator.TestValidateAsync(new Model(overMax));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.TooLong);
    }

    [Test]
    public async Task ValidString_CustomMaxLength_LengthAtCustomMax_NoError()
    {
        const int customMax = 10;
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: true, maxLength: customMax);
        var atMax = new string('a', customMax);

        var result = await validator.TestValidateAsync(new Model(atMax));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task ValidString_CustomMaxLength_LengthOverCustomMax_ProducesTooLongMessage()
    {
        const int customMax = 10;
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: true, maxLength: customMax);
        var overMax = new string('a', customMax + 1);

        var result = await validator.TestValidateAsync(new Model(overMax));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.TooLong);
    }

    [Test]
    public async Task ValidString_RequiredFalse_LengthOverMax_ProducesTooLongMessage()
    {
        // Pins that MaximumLength is unconditional — applies even when required:false.
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).ValidString(required: false);
        var overMax = new string('a', ApplicationConstants.DefaultMaxStringLength + 1);

        var result = await validator.TestValidateAsync(new Model(overMax));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.TooLong);
    }
}
