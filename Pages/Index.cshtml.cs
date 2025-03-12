using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;
using System.Linq;

namespace Valuator.Pages
{
    public class IndexModel : PageModel
    {
        private readonly RedisService _redisService;

        public IndexModel(RedisService redisService)
        {
            _redisService = redisService;
        }

        [BindProperty]
        public string UserText { get; set; } = string.Empty;

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(UserText))
            {
                ModelState.AddModelError("", "Текст не может быть пустым.");
                return Page();
            }

            string id = Guid.NewGuid().ToString();

            double rank = CalculateRank(UserText);
            double similarity = _redisService.IsDuplicateText(UserText) ? 1.0 : 0.0;

            _redisService.SaveRank(id, rank);
            _redisService.SaveSimilarity(id, similarity);
            _redisService.SaveProcessedText(UserText);

            return RedirectToPage("/Summary", new { id });
        }

        private double CalculateRank(string text)
        {
            int totalChars = text.Length;
            int alphaChars = text.Count(char.IsLetter);
            int nonAlphaChars = totalChars - alphaChars;

            return totalChars > 0 ? (double)nonAlphaChars / totalChars : 0.0;
        }
    }
}
