using Etmen_BLL.Repositories.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    [Authorize]
    [Route("api/chatbot/[action]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Ask([FromBody] ChatbotRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("[Controller] Ask action called");

                if (request == null)
                {
                    _logger.LogWarning("[Controller] Request is null");
                    return BadRequest(new { success = false, reply = "الطلب فارغ" });
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    _logger.LogWarning("[Controller] Message is empty");
                    return BadRequest(new { success = false, reply = "الرجاء كتابة سؤالك أولاً." });
                }

                _logger.LogInformation($"[Controller] Processing message: {request.Message.Substring(0, Math.Min(50, request.Message.Length))}...");

                var reply = await _chatbotService.AskAsync(request.Message, cancellationToken);

                _logger.LogInformation("[Controller] Service returned successfully");

                return Ok(new { success = true, reply });
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError($"[Controller] Operation cancelled: {ex.Message}");
                return StatusCode(408, new { success = false, reply = "انتهت مهلة الانتظار. حاول مرة أخرى." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Controller] Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { success = false, reply = "حدث خطأ في الخادم. حاول مرة أخرى لاحقاً." });
            }
        }

        /// <summary>
        /// Health check endpoint - returns the current API configuration status
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            try
            {
                return Ok(new
                {
                    status = "healthy",
                    service = "ChatbotService",
                    message = "Chatbot service is running"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[HealthCheck] Error: {ex.Message}");
                return StatusCode(500, new { status = "unhealthy", error = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint to validate API credentials and connection
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Test([FromBody] TestRequest testRequest, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("[Test] Test endpoint called");

                if (testRequest == null || string.IsNullOrWhiteSpace(testRequest.Message))
                {
                    return BadRequest(new { success = false, error = "Test message is required" });
                }

                _logger.LogInformation($"[Test] Testing with message: {testRequest.Message}");

                var reply = await _chatbotService.AskAsync(testRequest.Message, cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = testRequest.Message,
                    reply = reply,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Test] Error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    public class ChatbotRequest
    {
        public string? Message { get; set; }
    }

    public class TestRequest
    {
        public string? Message { get; set; }
    }
}
