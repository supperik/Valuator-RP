using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Valuator.Services;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly RedisService _redisService;

    public SummaryModel(ILogger<SummaryModel> logger, RedisService redisService)
    {
        _logger = logger;
        _redisService = redisService;
    }

    public string Id { get; set; } = string.Empty;
    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        if (!_redisService.IsConnected())
        {
            _logger.LogError("Ошибка: Нет подключения к Redis!");
            Rank = 0.0;
            Similarity = 0.0;
            return;
        }

        Id = id ?? "Не указан";
        _logger.LogInformation($"Запрос данных для ID: {id}");

        if (!string.IsNullOrEmpty(id))
        {
            Rank = _redisService.GetRank(id) ?? 0.0;
            Similarity = _redisService.GetSimilarity(id) ?? 0.0;

            _logger.LogInformation($"Ответ получен!");
        }
        else
        {
            Rank = 0.0;
            Similarity = 0.0;
        }
    }

}
