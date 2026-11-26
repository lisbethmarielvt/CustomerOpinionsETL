using System.Globalization;
using System.Text;
using CsvHelper;
using CustomerOpinionsETL.Application;
using CustomerOpinionsETL.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CustomerOpinionsETL.Infrastructure.Extractors;

public class CsvExtractor : IExtractor
{
    private readonly ILogger<CsvExtractor> _logger;
    private readonly IConfiguration _configuration;

    public string Name => "CSV";

    public CsvExtractor(
        ILogger<CsvExtractor> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<OpinionRecord>> ExtractAsync(
        CancellationToken cancellationToken)
    {
        var records = new List<OpinionRecord>();

        var path = _configuration["Sources:Csv:Path"]
                   ?? "Data/opiniones.csv";

        _logger.LogInformation(
            "Iniciando extracción desde CSV: {Path}", path);

        try
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo CSV.", path);
            }

            using var reader = new StreamReader(
                path, Encoding.UTF8);

            using var csv = new CsvReader(
                reader, CultureInfo.InvariantCulture);

            await foreach (var record in
                csv.GetRecordsAsync<OpinionRecord>(
                    cancellationToken))
            {
                records.Add(record);
            }

            _logger.LogInformation(
                "Extracción CSV completada. Registros: {Count}",
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
                "Error durante la extracción del CSV.");

            throw;
        }
    }
}