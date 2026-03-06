using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Features.Identity.ImportData;

// Extensibility interface for data importers.
// Register a new importer by creating a class that implements IDataImporter —
// Scrutor in Program.cs picks it up automatically without any code changes.
public interface IDataImporter
{
    // Identifier matched against the client-supplied "source" query parameter.
    string Source { get; }

    Task<Result<ImportResult>> ImportAsync(Stream fileStream, string fileName, Guid userId, CancellationToken ct);
}
