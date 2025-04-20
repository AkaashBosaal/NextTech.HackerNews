using NextTech.HackerNews.Api;
using NextTech.HackerNews.Core.Configurations;
using NextTech.HackerNews.Core.Interfaces.Infrastructure;
using NextTech.HackerNews.Core.Interfaces.Services;
using NextTech.HackerNews.Infrastructure;
using NextTech.HackerNews.Infrastructure.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });


        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache();
        builder.Services.AddLogging();

        builder.Services.AddApi();
        builder.Services.AddScoped<IHttpClientWrapper, HttpClientWrapper>();
        builder.Services.AddScoped<IHackerNewsService, HackerNewsService>();
        builder.Services.AddOptions<HackerNewsOptions>()
            .BindConfiguration(HackerNewsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors("AllowAll");
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}