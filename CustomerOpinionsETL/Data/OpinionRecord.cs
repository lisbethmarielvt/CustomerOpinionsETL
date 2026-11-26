namespace CustomerOpinionsETL.Domain;

public class OpinionRecord
{
    public string OpinionId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime OpinionDate { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string? EventId { get; set; }
    public string Sentiment { get; set; } = string.Empty;
}