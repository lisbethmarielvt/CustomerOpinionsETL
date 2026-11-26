using CustomerOpinionsETL;
using CustomerOpinionsETL.Application;
using CustomerOpinionsETL.Infrastructure.Extractors;
using CustomerOpinionsETL.Infrastructure.Storage;
using CustomerOpinionsETL.Infrastructure.Transformers;
using CustomerOpinionsETL.Infrastructure.Loaders;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IExtractor, CsvExtractor>();
builder.Services.AddSingleton<IExtractor, DatabaseExtractor>();
builder.Services.AddSingleton<IExtractor, ApiExtractor>();

builder.Services.AddSingleton<StagingService>();
builder.Services.AddSingleton<DimensionTransformer>();
builder.Services.AddSingleton<DimensionLoader>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();