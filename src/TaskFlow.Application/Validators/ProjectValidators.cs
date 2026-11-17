using FluentValidation;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Validators;

internal static class ProjectRules
{
    public const string ColorPattern = "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$";
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ColorCode)
            .Matches(ProjectRules.ColorPattern)
            .WithMessage("ColorCode must be a hex colour such as #3B82F6 or #FFF.")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorCode));
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ColorCode)
            .Matches(ProjectRules.ColorPattern)
            .WithMessage("ColorCode must be a hex colour such as #3B82F6 or #FFF.")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorCode));
    }
}
