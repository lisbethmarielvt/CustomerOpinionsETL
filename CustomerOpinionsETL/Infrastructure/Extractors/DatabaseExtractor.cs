using CustomerOpinionsETL.Application;
using CustomerOpinionsETL.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CustomerOpinionsETL.Infrastructure.Extractors;

public class DatabaseExtractor : IExtractor
{
    private readonly ILogger<DatabaseExtractor> _logger;
    private readonly IConfiguration _configuration;

    public string Name => "Database";

    public DatabaseExtractor(
        ILogger<DatabaseExtractor> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<OpinionRecord>> ExtractAsync(
        CancellationToken cancellationToken)
    {
        var records = new List<OpinionRecord>();

        try
        {
            var connectionString =
                _configuration.GetConnectionString("SourceDatabase")
                ?? throw new InvalidOperationException(
                    "Cadena de conexión no configurada.");

            _logger.LogInformation(
                "Iniciando extracción desde MySQL.");

            await using var connection =
                new MySqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            const string query = """
                SELECT OpinionId, CustomerId, ProductId,
                       Comment, Rating, OpinionDate,
                       Channel, EventId, Sentiment
                FROM reviews
                """;

            await using var command =
                new MySqlCommand(query, connection);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(new OpinionRecord
                {
                    OpinionId = reader.GetString(0),
                    CustomerId = reader.GetString(1),
                    ProductId = reader.GetString(2),
                    Comment = reader.GetString(3),
                    Rating = reader.GetInt32(4),
                    OpinionDate = reader.GetDateTime(5),
                    Channel = reader.GetString(6),
                    EventId = reader.IsDBNull(7)
                        ? null : reader.GetString(7),
                    Sentiment = reader.GetString(8)
                });
            }

            _logger.LogInformation(
                "Extracción MySQL completada. Registros: {Count}",
                records.Count);

            return records;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error durante la extracción desde MySQL.");

            throw;
        }
    }
}