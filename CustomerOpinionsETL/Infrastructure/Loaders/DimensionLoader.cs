using CustomerOpinionsETL.Domain;
using MySqlConnector;

namespace CustomerOpinionsETL.Infrastructure.Loaders;

public class DimensionLoader
{
    private readonly ILogger<DimensionLoader> _logger;
    private readonly IConfiguration _configuration;

    public DimensionLoader(
        ILogger<DimensionLoader> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task LoadAsync(
        IReadOnlyList<OpinionRecord> records,
        CancellationToken cancellationToken)
    {
        var connectionString =
            _configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException(
                "Conexión al Data Warehouse no configurada.");

        await using var connection =
            new MySqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            foreach (var record in records)
            {
                await InsertAsync(connection, transaction,
                    "DimCustomer", "CustomerId",
                    record.CustomerId, cancellationToken);

                await InsertAsync(connection, transaction,
                    "DimProduct", "ProductId",
                    record.ProductId, cancellationToken);

                await InsertAsync(connection, transaction,
                    "DimChannel", "ChannelName",
                    record.Channel, cancellationToken);

                await InsertAsync(connection, transaction,
                    "DimSentiment", "SentimentName",
                    record.Sentiment, cancellationToken);

                await InsertAsync(connection, transaction,
                    "DimEvent", "EventId",
                    record.EventId ?? "Desconocido",
                    cancellationToken);

                if (record.OpinionDate != DateTime.MinValue)
                {
                    await InsertDateAsync(
                        connection, transaction,
                        record.OpinionDate,
                        cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Carga de las seis dimensiones completada correctamente.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _logger.LogError(ex,
                "Error durante la carga de dimensiones.");

            throw;
        }
    }

    private static async Task InsertAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string table,
        string column,
        string value,
        CancellationToken cancellationToken)
    {
        var query = $"""
            INSERT INTO {table} ({column})
            VALUES (@value)
            ON DUPLICATE KEY UPDATE {column} = {column};
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue("@value", value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertDateAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        DateTime date,
        CancellationToken cancellationToken)
    {
        const string query = """
            INSERT INTO DimDate
            (DateKey, FullDate, DayNumber,
             MonthNumber, QuarterNumber, YearNumber)
            VALUES
            (@key, @date, @day, @month, @quarter, @year)
            ON DUPLICATE KEY UPDATE DateKey = DateKey;
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        var dateKey =
            date.Year * 10000 + date.Month * 100 + date.Day;

        command.Parameters.AddWithValue("@key", dateKey);
        command.Parameters.AddWithValue("@date", date.Date);
        command.Parameters.AddWithValue("@day", date.Day);
        command.Parameters.AddWithValue("@month", date.Month);
        command.Parameters.AddWithValue(
            "@quarter", (date.Month - 1) / 3 + 1);
        command.Parameters.AddWithValue("@year", date.Year);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}