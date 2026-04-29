using FluentValidation;
using FluentValidation.TestHelper;
using Shoplists.Application.Common.Validation;

namespace Shoplists.Application.UnitTests.Common.Validation.FluentValidationExtensionsTests;

public sealed class NotNullWithErrorCodeTests
{
    private sealed record Model(string? Value);

    [Test]
    public async Task NotNullWithErrorCode_ValueIsNotNull_NoError()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).NotNullWithErrorCode();

        var result = await validator.TestValidateAsync(new Model("anything"));

        result.ShouldNotHaveValidationErrorFor(m => m.Value);
    }

    [Test]
    public async Task NotNullWithErrorCode_ValueIsNull_ProducesRequiredErrorMessage()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).NotNullWithErrorCode();

        var result = await validator.TestValidateAsync(new Model(Value: null));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Required);
    }

    [Test]
    public async Task NotNullWithErrorCode_CustomErrorCode_UsesProvidedMessage()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(m => m.Value).NotNullWithErrorCode(ErrorCodes.Invalid);

        var result = await validator.TestValidateAsync(new Model(Value: null));

        result.ShouldHaveValidationErrorFor(m => m.Value).WithErrorMessage(ErrorCodes.Invalid);
    }
}
