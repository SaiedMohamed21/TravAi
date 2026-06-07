using System.Text;
using System.Text.Json;
using TravAi.DTOs.AI;

namespace TravAi.Services.AI
{
    /// <summary>
    /// Proxies chat requests to the Python FastAPI AI microservice.
    /// Uses HttpClient to communicate with the Python service running on a separate port.
    /// </summary>
    public class AiChatbotService : IAiChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiChatbotService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AiChatbotService(HttpClient httpClient, ILogger<AiChatbotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
            };
        }

        /// <inheritdoc />
        public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request)
        {
            try
            {
                // Build the request body matching Python's ChatRequest schema
                var pythonRequest = new
                {
                    message = request.Message,
                    conversation_history = request.ConversationHistory
                        .TakeLast(10) // Only send last 10 messages as per requirements
                        .Select(h => new { role = h.Role, content = h.Content })
                        .ToList(),
                    city_context = request.CityContext
                };

                var jsonContent = JsonSerializer.Serialize(pythonRequest, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "Sending chat request to Python AI service. Message length: {Length}, City: {City}",
                    request.Message.Length,
                    request.CityContext ?? "All cities");

                var response = await _httpClient.PostAsync("/api/chat", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Python AI service returned {StatusCode}: {Body}",
                        response.StatusCode, errorBody);

                    return new ChatResponseDto
                    {
                        Success = false,
                        Error = $"AI service error: {response.StatusCode}",
                        Message = new ChatMessageDto
                        {
                            Role = "assistant",
                            Content = "Sorry, I'm having trouble connecting to the AI service right now. Please try again later.",
                            LanguageDetected = "en"
                        }
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ChatResponseDto>(responseJson, _jsonOptions);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize Python AI response");
                    return new ChatResponseDto
                    {
                        Success = false,
                        Error = "Failed to parse AI response"
                    };
                }

                _logger.LogInformation(
                    "AI chat response received. Language: {Lang}, Tours: {Count}",
                    result.Message?.LanguageDetected ?? "unknown",
                    result.Message?.RetrievedTours?.Count ?? 0);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to Python AI service");
                return new ChatResponseDto
                {
                    Success = false,
                    Error = "AI service is not available. Make sure the Python backend is running.",
                    Message = new ChatMessageDto
                    {
                        Role = "assistant",
                        Content = "⚠️ The AI service is currently offline. Please ensure the Python backend is running on the configured port.",
                        LanguageDetected = "en"
                    }
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to Python AI service timed out");
                return new ChatResponseDto
                {
                    Success = false,
                    Error = "AI service request timed out",
                    Message = new ChatMessageDto
                    {
                        Role = "assistant",
                        Content = "⏱️ The request took too long. Please try a shorter question or try again.",
                        LanguageDetected = "en"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AiChatbotService");
                return new ChatResponseDto
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
