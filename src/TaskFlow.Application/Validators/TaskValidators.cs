using FluentValidation;
using TaskFlow.Application.Common.Extensions;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ProjectId).NotEqual(Guid.Empty).When(x => x.ProjectId.HasValue);
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ProjectId).NotEqual(Guid.Empty).When(x => x.ProjectId.HasValue);
    }
}

public sealed class ChangeTaskStatusRequestValidator : AbstractValidator<ChangeTaskStatusRequest>
{
    public ChangeTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class TaskFilterRequestValidator : AbstractValidator<TaskFilterRequest>
{
    public TaskFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority.HasValue);
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.DueTo)
            .Must((request, dueTo) => dueTo!.Value.ToUtc() >= request.DueFrom!.Value.ToUtc())
            .WithMessage("DueTo must be greater than or equal to DueFrom.")
            .When(x => x.DueFrom.HasValue && x.DueTo.HasValue);
    }
}
