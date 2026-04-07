using FluentValidation;
using Shoplists.Application.Common.Constants;

namespace Shoplists.Application.Common.Validation;

internal static class FluentValidationExtensions
{
    internal static IRuleBuilderOptions<T, TProperty?> NotNullWithErrorCode<T, TProperty>(
        this IRuleBuilder<T, TProperty?> ruleBuilder,
        string errorCode = ErrorCodes.Required
    ) => ruleBuilder.NotNull().WithMessage(errorCode);

    internal static IRuleBuilderOptions<T, TProperty> NotEmptyWithErrorCode<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder,
        string errorCode = ErrorCodes.Required
    ) => ruleBuilder.NotEmpty().WithMessage(errorCode);

    internal static IRuleBuilderOptions<T, string?> ValidString<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        bool required,
        int maxLength = ApplicationConstants.DefaultMaxStringLength
    )
    {
        if (required)
            ruleBuilder = ruleBuilder.NotEmptyWithErrorCode();

        return ruleBuilder.MaximumLength(maxLength).WithMessage(ErrorCodes.TooLong);
    }
}
