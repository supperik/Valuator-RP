using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;

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
            _logger.LogInformation($"[LOG] Новый пользователь зашел на сайт с IP: {userIp} в {DateTime.Now}");
        }

        [BindProperty]

        public string UserText { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("[CONSOLE] Начало обработки...");
            if (string.IsNullOrWhiteSpace(UserText))
            {
                ModelState.AddModelError(string.Empty, "Текст не может быть пустым.");
                return Page();
            }

            string id = Guid.NewGuid().ToString();
            string textKey = "TEXT-" + id;
            string textHash = GetTextHash(UserText);

            if (UserText == null)
            {
                ModelState.AddModelError(string.Empty, "Текст с ключом не найден!");
                return Page();
            }
            ModelState.AddModelError(string.Empty, "Текст с ключом найден!");

            

            await _redisService.SaveTextAsync(textKey, textHash);
            await _redisService.SaveProcessedTextAsync(UserText);

            try
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "rank_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: "similarity_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var message = new TextProcessingMessage
                {
                    Id = id,
                    UserText = UserText
                };
                var jsonMessage = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                channel.BasicPublish(exchange: "", routingKey: "rank_tasks", basicProperties: null, body: body);
                _logger.LogInformation($"Отправлено сообщение в очередь: {id}");

                channel.BasicPublish(exchange: "", routingKey: "similarity_tasks", basicProperties: null, body: body);
                _logger.LogInformation($"Отправлено сообщение в очередь Similarity: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения в очередь RabbitMQ.");
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
        public string UserText { get; set; }
    }
}
