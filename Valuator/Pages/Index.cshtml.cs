using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Threading.Channels;

namespace Valuator.Pages
{
    public class IndexModel : PageModel
    {
        private readonly RedisService _redisService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(RedisService redisService, ILogger<IndexModel> logger)
        {
            _redisService = redisService;
            _logger = logger;
        }

        public void OnGet()
        {
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            _logger.LogInformation($"[LOG] ����� ������������ ����� �� ���� � IP: {userIp} � {DateTime.Now}");
        }

        [BindProperty]

        public string UserText { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("[CONSOLE] ������ ���������...");
            if (string.IsNullOrWhiteSpace(UserText))
            {
                ModelState.AddModelError(string.Empty, "����� �� ����� ���� ������.");
                return Page();
            }

            string id = Guid.NewGuid().ToString();
            string textKey = "TEXT-" + id;
            string unhashedTextKey = "UNHASHED-TEXT-" + id;
            string textHash = GetTextHash(UserText);

            if (UserText == null)
            {
                ModelState.AddModelError(string.Empty, "����� � ������ �� ������!");
                return Page();
            }
            ModelState.AddModelError(string.Empty, "����� � ������ ������!");

            await _redisService.SaveTextAsync(textKey, textHash);
            await _redisService.SaveTextAsync(unhashedTextKey, UserText);
            await _redisService.SaveProcessedTextAsync(UserText);

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            try
            {
                

                channel.QueueDeclare(queue: "rank_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: "similarity_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var message = new TextProcessingMessage
                {
                    Id = id
                };
                var jsonMessage = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                channel.BasicPublish(exchange: "", routingKey: "rank_tasks", basicProperties: null, body: body);
                _logger.LogInformation($"���������� ��������� � �������: {id}");

                channel.BasicPublish(exchange: "", routingKey: "similarity_tasks", basicProperties: null, body: body);
                _logger.LogInformation($"���������� ��������� � ������� Similarity: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� �������� ��������� � ������� RabbitMQ.");
            }

            try
            {
                double similarity = await _redisService.IsDuplicateTextAsync(textHash) ? 1.0 : 0.0;
                string similarityKey = "SIMILARITY-" + id;

                await _redisService.SaveSimilarityAsync(similarityKey, similarity);

                Console.WriteLine($"[CONSOLE] ��������� ���������� ��� ������ � ID: {id} | Similarity: {similarity}");

                var eventMessage = new
                {
                    EventType = "SimilarityCalculated",
                    Id = "TEXT" + id,
                    Similarity = similarity
                };
                string json = JsonConvert.SerializeObject(eventMessage);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: "events", routingKey: "similarity_events", basicProperties: null, body: body);
            }
            catch (Exception ex)
            {
                var eventMessage = new
                {
                    EventType = "SimilarityCalculatedError",
                    Id = "TEXT" + id,
                    Similarity = "null"
                };
                string json = JsonConvert.SerializeObject(eventMessage);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: "events", routingKey: "similarity_events", basicProperties: null, body: body);

                Console.WriteLine($"[CONSOLE] ������ ���������� rank � similarity: {ex.Message}");
            }

            return RedirectToPage("/Summary", new { id });
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
    public class TextProcessingMessage
    {
        public string Id { get; set; }
    }
}
