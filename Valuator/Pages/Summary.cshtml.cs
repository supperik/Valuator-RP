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
    public string? RankDisplay { get; set; }
    public string? SimilarityDisplay { get; set; }

    public async Task OnGet(string id)
    {
        if (!_redisService.IsConnected())
        {
            _logger.LogError("Ошибка: Нет подключения к Redis!");
            return;
        }

        Id = id ?? "Не указан";
        _logger.LogInformation($"Запрос данных для ID: {id}");

        if (!string.IsNullOrEmpty(id))
        {
            var rank = await _redisService.GetRankAsync("RANK-" + id);
            var similarity = await _redisService.GetSimilarityAsync("SIMILARITY-" + id);

            string? message = null;

            if (rank == null)
            {
                message = "Оценка содержания не завершена";
                RankDisplay = message;
                
            }
            else
            {
                RankDisplay = rank.ToString();  
            }

            if (similarity == null)
            {
                message = "Оценка содержания не завершена";
                SimilarityDisplay = message;
            }
            else
            {
                SimilarityDisplay = similarity.ToString();
            }

            _logger.LogInformation($"Ответ получен!");
        }
        else
        {
            RankDisplay = 0.0.ToString();
            SimilarityDisplay = 0.0.ToString();
        }
    }

}
