using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Features.Identity.ImportData;

// Dispatches the import file to the appropriate IDataImporter implementation based on the source parameter.
public sealed class ImportDataHandler(IEnumerable<IDataImporter> importers, ILogger<ImportDataHandler> logger)
{
    public async Task<Result<ImportResult>> HandleAsync(
        Stream fileStream, string fileName, string source, Guid userId, CancellationToken ct)
    {
        var normalizedSource = source.ToLowerInvariant();

        // Find the importer registered for this source
        var importer = importers.FirstOrDefault(i => i.Source == normalizedSource);
        if (importer is null)
            return Error.Validation($"No importer found for source '{source}'.");

        logger.ImportStarted(normalizedSource, fileName, userId);

        try
        {
            var result = await importer.ImportAsync(fileStream, fileName, userId, ct);

            if (result.IsSuccess)
            {
                logger.ImportCompleted(
                    result.Value.Source,
                    result.Value.Format,
                    result.Value.Counts.Wallets,
                    result.Value.Counts.Transactions);

                if (result.Value.Skipped.SharedWalletMembers > 0)
                    logger.ImportSkippedMembers(result.Value.Skipped.SharedWalletMembers);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.ImportFailed(normalizedSource, fileName, ex);
            return Error.Internal("Import failed due to an unexpected error.");
        }
    }
}
