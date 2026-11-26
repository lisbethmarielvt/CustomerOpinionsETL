using System.Net.Http.Json;
using CustomerOpinionsETL.Application;
using CustomerOpinionsETL.Domain;

namespace CustomerOpinionsETL.Infrastructure.Extractors;

public class ApiExtractor : IExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiExtractor> _logger;
    private readonly IConfiguration _configuration;

    public string Name => "API";

    public ApiExtractor(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiExtractor> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<OpinionRecord>> ExtractAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando extracción desde API REST.");

            var url = _configuration["Sources:Api:Url"]
                ?? "https://jsonplaceholder.typicode.com/comments?postId=1";

            using var client = _httpClientFactory.CreateClient();

            var comments = await client
                .GetFromJsonAsync<List<ApiComment>>(
                    url, cancellationToken);

            var records = new List<OpinionRecord>();

            foreach (var comment in comments ?? new List<ApiComment>())
            {
                records.Add(new OpinionRecord
                {
                    OpinionId = $"API{comment.Id:D3}",
                    CustomerId = string.Empty,
                    ProductId = string.Empty,
                    Comment = comment.Body,
                    Rating = 0,
                    OpinionDate = DateTime.MinValue,
                    Channel = "API",
                    EventId = null,
                    Sentiment = string.Empty
                });
            }

            _logger.LogInformation(
                "Extracción API completada. Registros: {Count}",
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
                "Error durante la extracción desde API REST.");

            throw;
        }
    }

    public class ApiComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}