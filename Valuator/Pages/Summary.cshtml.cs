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
    public double? Rank { get; set; }
    public double? Similarity { get; set; }
    public string Message { get; set; }

    public async Task OnGet(string id)
    {
        if (!_redisService.IsConnected())
        {
            _logger.LogError("Ошибка: Нет подключения к Redis!");
            Rank = null;
            return;
        }

        Id = id ?? "Не указан";
        _logger.LogInformation($"Запрос данных для ID: {id}");

        if (!string.IsNullOrEmpty(id))
        {
            Rank = await _redisService.GetRankAsync("RANK-" + id);
            Similarity = _redisService.GetSimilarity("SIMILARITY-" + id);

            if (Rank == null)
            {
                Message = "Оценка содержания не завершена";
            }
            else
            {
                Message = $"Оценка содержания: {Rank}";
            }

            _logger.LogInformation($"Ответ получен!");
        }
        else
        {
            Rank = 0.0;
            Similarity = 0.0;
        }
    }

}
