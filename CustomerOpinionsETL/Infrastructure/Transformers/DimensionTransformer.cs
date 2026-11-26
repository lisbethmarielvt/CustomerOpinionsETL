using CustomerOpinionsETL.Domain;

namespace CustomerOpinionsETL.Infrastructure.Transformers;

public class DimensionTransformer
{
    private readonly ILogger<DimensionTransformer> _logger;

    public DimensionTransformer(
        ILogger<DimensionTransformer> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<OpinionRecord> Transform(
        IEnumerable<OpinionRecord> records)
    {
        _logger.LogInformation(
            "Iniciando transformación de datos.");

        var transformed = records
            .Where(record =>
                !string.IsNullOrWhiteSpace(record.OpinionId))
            .Select(record => new OpinionRecord
            {
                OpinionId = Normalize(record.OpinionId),
                CustomerId = Normalize(record.CustomerId),
                ProductId = Normalize(record.ProductId),
                Comment = record.Comment?.Trim() ?? string.Empty,
                Rating = record.Rating,
                OpinionDate = record.OpinionDate.Date,
                Channel = Normalize(record.Channel),
                EventId = Normalize(record.EventId),
                Sentiment = Normalize(record.Sentiment)
            })
            .DistinctBy(record => record.OpinionId)
            .ToList();

        _logger.LogInformation(
            "Transformación completada. Registros: {Count}",
            transformed.Count);

        return transformed;
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Desconocido"
            : value.Trim();
    }
}