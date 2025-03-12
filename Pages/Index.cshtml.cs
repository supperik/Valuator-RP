using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;
using System.Text;

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

        public void OnGet()
        {

        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(UserText))
            {
                ModelState.AddModelError("", "Текст не может быть пустым.");
                return Page();
            }

            string id = Guid.NewGuid().ToString();

            double rank = CalculateRank(UserText);

            string textHash = GetTextHash(UserText);
            double similarity = _redisService.IsDuplicateText(UserText) ? 1.0 : 0.0;

            string textKey = "TEXT-" + id;
            string rankKey = "RANK-" + id;
            string similarityKey = "SIMILARITY-" + id;

            _redisService.SaveText(textKey, UserText);
            _redisService.SaveRank(rankKey, rank);
            _redisService.SaveSimilarity(similarityKey, similarity);
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
        private string GetTextHash(string text)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
