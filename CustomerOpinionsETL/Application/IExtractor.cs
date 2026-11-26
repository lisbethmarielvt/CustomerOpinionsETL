using CustomerOpinionsETL.Domain;

namespace CustomerOpinionsETL.Application;

public interface IExtractor
{
    string Name { get; }

    Task<IReadOnlyList<OpinionRecord>> ExtractAsync(
        CancellationToken cancellationToken);
}