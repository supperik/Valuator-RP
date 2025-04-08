using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;
using System.Text;
using RabbitMQ.Client;

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

            Console.WriteLine($"[CONSOLE] Пользователь с IP {userIp} зашел на сайт в {DateTime.Now}");
        }

        [BindProperty]

        public string UserText { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UserText))
            {
                ModelState.AddModelError(string.Empty, "Текст не может быть пустым.");
                return Page();
            }

            string id = Guid.NewGuid().ToString();
            string textKey = "TEXT-" + id;

            await _redisService.SaveTextAsync(textKey, UserText);

            try
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "rank_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var body = Encoding.UTF8.GetBytes(id);
                channel.BasicPublish(exchange: "", routingKey: "rank_tasks", basicProperties: null, body: body);

                _logger.LogInformation($"Отправлено сообщение в очередь: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения в очередь RabbitMQ.");
            }

            return RedirectToPage("/Summary", new { id });
        }
    }
}
