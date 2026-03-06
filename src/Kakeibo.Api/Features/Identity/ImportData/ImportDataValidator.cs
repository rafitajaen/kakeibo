using FluentValidation;

namespace Kakeibo.Api.Features.Identity.ImportData;

public sealed class ImportDataValidator : AbstractValidator<ImportDataEndpoint.ImportDataRequest>
{
    private static readonly string[] ValidSources = ["kakeibo", "onemoney"];

    // Max file size: 200 MB
    private const long MaxFileSizeBytes = 200L * 1024 * 1024;

    public ImportDataValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty()
            .Must(s => ValidSources.Contains(s.ToLowerInvariant()))
            .WithMessage("Source must be 'kakeibo' or 'onemoney'.");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("A file must be provided.")
            .Must(f => f is not null && f.Length > 0)
            .WithMessage("File must not be empty.")
            .Must(f => f is null || f.Length <= MaxFileSizeBytes)
            .WithMessage("File must not exceed 200 MB.");
    }
}
