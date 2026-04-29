using FluentValidation;
using FluentValidation.TestHelper;
using Shoplists.Application.Common.Validation;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.UnitTests.Common.Validation.FluentValidationExtensionsTests;

public sealed class NotEmptyWithErrorCodeTests
{
    private sealed record StringModel(string? Value);

    private sealed record IntModel(int Value);

    private sealed record GuidModel(Guid Value);

    [Test]
    public async Task NotEmptyWithErrorCode_NonEmptyString_NoError()
    {
        var validator = new InlineValidator<StringModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode();

        var result = await validator.TestValidateAsync(new StringModel("anything"));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task NotEmptyWithErrorCode_NullEmptyOrWhitespaceString_ProducesRequiredMessage(
        string? value
    )
    {
        var validator = new InlineValidator<StringModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode();

        var result = await validator.TestValidateAsync(new StringModel(value));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task NotEmptyWithErrorCode_DefaultInt_ProducesRequiredMessage()
    {
        var validator = new InlineValidator<IntModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode();

        var result = await validator.TestValidateAsync(new IntModel(0));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task NotEmptyWithErrorCode_NonZeroInt_NoError()
    {
        var validator = new InlineValidator<IntModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode();

        var result = await validator.TestValidateAsync(new IntModel(1));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task NotEmptyWithErrorCode_EmptyGuid_ProducesRequiredMessage()
    {
        var validator = new InlineValidator<GuidModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode();

        var result = await validator.TestValidateAsync(new GuidModel(Guid.Empty));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task NotEmptyWithErrorCode_NonEmptyGuid_NoError()
    {
        var validator = new InlineValidator<GuidModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode();

        var result = await validator.TestValidateAsync(new GuidModel(Guid.NewGuid()));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task NotEmptyWithErrorCode_CustomErrorCode_UsesProvidedMessage()
    {
        var validator = new InlineValidator<StringModel>();
        validator.RuleFor(m => m.Value).NotEmptyWithErrorCode(ErrorCodes.Invalid);

        var result = await validator.TestValidateAsync(new StringModel(""));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Invalid);
    }
}
