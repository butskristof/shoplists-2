using FluentValidation;

namespace Shoplists.Application.Common.Validation;

internal abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
    }
}
