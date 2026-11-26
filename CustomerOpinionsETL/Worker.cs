using System.Diagnostics;
using CustomerOpinionsETL.Application;
using CustomerOpinionsETL.Domain;
using CustomerOpinionsETL.Infrastructure.Storage;
using CustomerOpinionsETL.Infrastructure.Transformers;
using CustomerOpinionsETL.Infrastructure.Loaders;

namespace CustomerOpinionsETL;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEnumerable<IExtractor> _extractors;
    private readonly StagingService _stagingService;
    private readonly DimensionTransformer _transformer;
    private readonly DimensionLoader _loader;

    public Worker(
        ILogger<Worker> logger,
        IEnumerable<IExtractor> extractors,
        StagingService stagingService,
        DimensionTransformer transformer,
        DimensionLoader loader)
    {
        _logger = logger;
        _extractors = extractors;
        _stagingService = stagingService;
        _transformer = transformer;
        _loader = loader;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Iniciando proceso ETL.");

            // E - Extracción
            var tasks = _extractors.Select(async extractor =>
            {
                var records = await extractor.ExtractAsync(
                    stoppingToken);

                await _stagingService.SaveAsync(
                    extractor.Name,
                    records,
                    stoppingToken);

                _logger.LogInformation(
                    "Fuente: {Source}. Registros: {Count}",
                    extractor.Name,
                    records.Count);

                return records;
            });

            var results = await Task.WhenAll(tasks);

            var allRecords = results
                .SelectMany(records => records)
                .ToList();

            _logger.LogInformation(
                "Total de registros extraídos: {Count}",
                allRecords.Count);

            // T - Transformación
            var transformed = _transformer.Transform(
                allRecords);

            _logger.LogInformation(
                "Total de registros transformados: {Count}",
                transformed.Count);

            // L - Carga de dimensiones
            await _loader.LoadAsync(
                transformed,
                stoppingToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Proceso ETL completado en {Time} ms.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Proceso ETL cancelado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error durante el proceso ETL.");
        }
    }
}