using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Etmen_BLL.Repositories.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly ChatbotApiOptions _options;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(
            HttpClient httpClient,
            IOptions<ChatbotApiOptions> options,
            ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> AskAsync(
            string message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return "Please write a health question first.";

                message = message.Trim();

                if (message.Length > 500)
                    return "Please keep your question shorter.";

                _logger.LogInformation($"[Chatbot] Processing message: {message.Substring(0, Math.Min(50, message.Length))}...");

                var systemInstruction =
     """
    You are Etmen Health Assistant, a helpful and detailed health advisor.

    Answer health and wellness questions only.

    Allowed topics:
    nutrition, healthy habits, fitness, exercise,
    sleep, mental wellness, hydration, healthy lifestyle,
    BMI, calories, vitamins, general medical awareness,
    preventive care, and wellness tips.

    Do NOT diagnose diseases or prescribe medications.
    Do NOT provide emergency medical advice.

    If the user asks about dangerous symptoms or emergencies,
    tell them to contact a doctor or emergency services immediately.

    If the user asks outside health and wellness,
    politely refuse and redirect to health topics.

    Response style:
    - Answer in the same language as the user (Arabic or English)
    - Give detailed, helpful, and practical answers (5 to 10 sentences)
    - Use bullet points or numbered lists when listing tips or steps
    - Be warm, supportive, and encouraging
    - Always end with a motivational tip or reminder
    """;

                var requestBody = new
                {
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = systemInstruction }
                        }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = message }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.6,      
                        maxOutputTokens = 1000  
                    }
                };

                var url = $"{_options.Endpoint}?key={_options.ApiKey}";
                _logger.LogDebug($"[Chatbot] API Endpoint: {url.Replace(_options.ApiKey, "***")}");

                var json = JsonSerializer.Serialize(requestBody);
                _logger.LogDebug($"[Chatbot] Request JSON length: {json.Length}");

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("[Chatbot] Sending request to Gemini API...");

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogDebug($"[Chatbot] Response Status: {response.StatusCode}");
                _logger.LogDebug($"[Chatbot] Response Content Length: {responseText.Length}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[Chatbot] Gemini API Error {response.StatusCode}: {responseText}");
                    return "The health assistant is not available right now. Please try again later.";
                }

                _logger.LogDebug($"[Chatbot] Response: {responseText.Substring(0, Math.Min(500, responseText.Length))}");

                return ExtractGeminiReply(responseText);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError($"[Chatbot] Request timeout: {ex.Message}");
                return "The request took too long. Please try again.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"[Chatbot] Network error: {ex.Message}");
                return "Network error occurred. Please check your connection and try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Chatbot] Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return "An unexpected error occurred. Please try again later.";
            }
        }

        private string ExtractGeminiReply(string responseText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogError("[Chatbot] Response text is empty");
                    return "The health assistant did not provide a response. Please try again.";
                }

                _logger.LogDebug($"[Chatbot] Parsing JSON response...");

                using var doc = JsonDocument.Parse(responseText);
                var root = doc.RootElement;

                // Debug: Log the structure
                _logger.LogDebug($"[Chatbot] Root element type: {root.ValueKind}");

                // Check for error response
                if (root.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetProperty("message").GetString() ?? "Unknown error";
                    _logger.LogError($"[Chatbot] Gemini API returned error: {errorMessage}");
                    return "The health assistant encountered an error. Please try again later.";
                }

                if (!root.TryGetProperty("candidates", out var candidates))
                {
                    _logger.LogError("[Chatbot] No 'candidates' field in response");
                    return "The health assistant could not process your question. Please try again.";
                }

                var candidatesLength = candidates.GetArrayLength();
                _logger.LogDebug($"[Chatbot] Found {candidatesLength} candidates");

                if (candidatesLength == 0)
                {
                    _logger.LogWarning("[Chatbot] Candidates array is empty");
                    return "The health assistant did not generate a response. Please try again.";
                }

                var firstCandidate = candidates[0];

                if (!firstCandidate.TryGetProperty("content", out var content))
                {
                    _logger.LogError("[Chatbot] First candidate has no 'content'");
                    return "The health assistant response format is invalid. Please try again.";
                }

                if (!content.TryGetProperty("parts", out var parts))
                {
                    _logger.LogError("[Chatbot] Content has no 'parts'");
                    return "The health assistant response format is invalid. Please try again.";
                }

                var partsLength = parts.GetArrayLength();
                _logger.LogDebug($"[Chatbot] Found {partsLength} parts in content");

                if (partsLength == 0)
                {
                    _logger.LogWarning("[Chatbot] Parts array is empty");
                    return "The health assistant did not provide text. Please try again.";
                }

                var firstPart = parts[0];

                if (!firstPart.TryGetProperty("text", out var text))
                {
                    _logger.LogError("[Chatbot] First part has no 'text'");
                    return "The health assistant response has no text. Please try again.";
                }

                var result = text.GetString();

                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("[Chatbot] Text content is empty");
                    return "The health assistant generated an empty response. Please try again.";
                }

                _logger.LogInformation($"[Chatbot] Successfully extracted response ({result.Length} characters)");
                return result;
            }
            catch (JsonException jex)
            {
                _logger.LogError($"[Chatbot] JSON parsing error: {jex.Message}");
                return "The health assistant returned an invalid response format. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Chatbot] Error parsing response: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                return "The health assistant encountered an error while processing the response. Please try again later.";
            }
        }
    }
}