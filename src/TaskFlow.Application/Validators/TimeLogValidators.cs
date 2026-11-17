using FluentValidation;
using TaskFlow.Application.Common.Extensions;
using TaskFlow.Application.DTOs.TimeLogs;

namespace TaskFlow.Application.Validators;

public sealed class StartTimerRequestValidator : AbstractValidator<StartTimerRequest>
{
    public StartTimerRequestValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class StopTimerRequestValidator : AbstractValidator<StopTimerRequest>
{
    public StopTimerRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class ManualTimeLogRequestValidator : AbstractValidator<ManualTimeLogRequest>
{
    private const double MaxMinutesPerEntry = 24 * 60;

    public ManualTimeLogRequestValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.StartTime)
            .NotEqual(default(DateTime)).WithMessage("StartTime is required.");

        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x)
            .Must(x => x.EndTime.HasValue != x.DurationInMinutes.HasValue)
            .WithMessage("Provide either EndTime or DurationInMinutes (exactly one of them).")
            .OverridePropertyName(nameof(ManualTimeLogRequest.EndTime));

        RuleFor(x => x.DurationInMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxMinutesPerEntry)
            .When(x => x.DurationInMinutes.HasValue);

        RuleFor(x => x.EndTime)
            .Must((request, endTime) => endTime!.Value.ToUtc() > request.StartTime.ToUtc())
            .WithMessage("EndTime must be later than StartTime.")
            .When(x => x.EndTime.HasValue);

        RuleFor(x => x.EndTime)
            .Must((request, endTime) => (endTime!.Value.ToUtc() - request.StartTime.ToUtc()).TotalMinutes <= MaxMinutesPerEntry)
            .WithMessage("A single time log cannot be longer than 24 hours.")
            .When(x => x.EndTime.HasValue && x.EndTime.Value.ToUtc() > x.StartTime.ToUtc());
    }
}
