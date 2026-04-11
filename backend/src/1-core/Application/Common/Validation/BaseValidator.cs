using FluentValidation;

namespace Shoplists.Application.Common.Validation;

/// <summary>
/// Serves as a base class for validators, providing a standardized configuration
/// for cascading behavior when applying multiple validation rules.
/// </summary>
/// <typeparam name="T">The type of the object being validated.</typeparam>
internal abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
    }
}
