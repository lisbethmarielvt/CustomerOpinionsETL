using System.Text.Json;
using CustomerOpinionsETL.Domain;

namespace CustomerOpinionsETL.Infrastructure.Storage;

public class StagingService
{
    private readonly ILogger<StagingService> _logger;
    private readonly IConfiguration _configuration;

    public StagingService(
        ILogger<StagingService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SaveAsync(
        string source,
        IReadOnlyList<OpinionRecord> records,
        CancellationToken cancellationToken)
    {
        var directory =
            _configuration["Staging:Path"] ?? "Staging";

        Directory.CreateDirectory(directory);

        var fileName =
            $"{source}_{Guid.NewGuid():N}.json";

        var path = Path.Combine(directory, fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await using var stream = File.Create(path);

        await JsonSerializer.SerializeAsync(
            stream, records, options, cancellationToken);

        _logger.LogInformation(
            "Staging: {Count} registros guardados en {Path}",
            records.Count, path);
    }
}