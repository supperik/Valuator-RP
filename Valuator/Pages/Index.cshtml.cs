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
            _logger.LogInformation($"[LOG] ����� ������������ ����� �� ���� � IP: {userIp} � {DateTime.Now}");

            Console.WriteLine($"[CONSOLE] ������������ � IP {userIp} ����� �� ���� � {DateTime.Now}");
        }

        [BindProperty]

        public string UserText { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UserText))
            {
                ModelState.AddModelError(string.Empty, "����� �� ����� ���� ������.");
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

                _logger.LogInformation($"���������� ��������� � �������: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� �������� ��������� � ������� RabbitMQ.");
            }

            return RedirectToPage("/Summary", new { id });
        }
    }
}
