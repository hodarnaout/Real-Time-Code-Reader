// ============================================
// FILE: BLL/Services/NanoBananaService.cs
// PURPOSE: FIXED - Real Image Generation Service using Gemini API (Nano Banana)
// ============================================

using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Real_Time_Code_Reader.BLL.Services
{
    public interface INanoBananaService
    {
        Task<byte[]> GenerateConceptVisualizationAsync(string concept, string studentLevel);
        Task<byte[]> GenerateCodeFlowchartAsync(string code, string language);
        Task<byte[]> GenerateInfographicAsync(string topic, string style);
        Task<byte[]> GenerateCheatSheetAsync(string topic, string language);
    }

    /// <summary>
    /// FIXED Service for generating images using Gemini's Nano Banana (Native Image Generation)
    /// Models: gemini-2.5-flash-image (fast) and gemini-3-pro-image-preview (professional)
    /// Documentation: https://ai.google.dev/gemini-api/docs/image-generation
    /// </summary>
    public class NanoBananaService : INanoBananaService
    {
        private readonly string _apiKey;
        private readonly string _flashImageModel;
        private readonly string _proImageModel;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NanoBananaService> _logger;
        private const string GEMINI_API_BASE = "https://generativelanguage.googleapis.com/v1beta/models";

        public NanoBananaService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<NanoBananaService> logger)
        {
            _apiKey = configuration["GeminiSettings:ApiKey"]
                ?? throw new ArgumentNullException("Gemini API Key not configured. Add GeminiSettings:ApiKey to appsettings.json");

            // Read from config or use defaults
            _flashImageModel = configuration["GeminiSettings:ImageModel"] ?? "gemini-2.5-flash-image";
            _proImageModel = configuration["GeminiSettings:ProImageModel"] ?? "gemini-3-pro-image-preview";

            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;

            _logger.LogInformation("NanoBananaService initialized with Flash Model: {FlashModel}, Pro Model: {ProModel}",
                _flashImageModel, _proImageModel);
        }

        /// <summary>
        /// Generate a visual representation of a programming concept
        /// </summary>
        public async Task<byte[]> GenerateConceptVisualizationAsync(string concept, string studentLevel)
        {
            var prompt = $"Create a vibrant, educational infographic that explains the programming concept '{concept}' " +
                $"for a {studentLevel} level student. The infographic should be colorful, engaging, and easy to understand. " +
                $"Include diagrams, examples, and clear explanations. Use a modern, professional design with good typography. " +
                $"Make it suitable for classroom use or study materials.";

            // Use flash model with 1K resolution for speed
            return await GenerateImageAsync(prompt, _flashImageModel, "1:1", "1K");
        }

        /// <summary>
        /// Generate a flowchart from code
        /// </summary>
        public async Task<byte[]> GenerateCodeFlowchartAsync(string code, string language)
        {
            // Truncate code if too long
            var truncatedCode = code.Length > 500 ? code.Substring(0, 500) + "..." : code;

            var prompt = $"Create a professional flowchart diagram that visualizes the logic flow of this {language} code:\n\n" +
                $"```{language}\n{truncatedCode}\n```\n\n" +
                $"Use standard flowchart symbols (rectangles for processes, diamonds for decisions, arrows for flow). " +
                $"Make it clear, well-organized, and easy to follow. Use a clean, professional design with good contrast and readability.";

            // Use flash model with 1K resolution for speed
            return await GenerateImageAsync(prompt, _flashImageModel, "16:9", "1K");
        }

        /// <summary>
        /// Generate an educational infographic
        /// </summary>
        public async Task<byte[]> GenerateInfographicAsync(string topic, string style)
        {
            var prompt = $"Create a comprehensive educational infographic about '{topic}'. " +
                $"Style: {style}. Include key concepts, visual examples, and clear explanations. " +
                $"Use a professional layout with excellent typography, cohesive color scheme, and clear visual hierarchy. " +
                $"Make it informative and visually appealing.";

            // Use Pro model with 2K resolution for better quality
            return await GenerateImageAsync(prompt, _proImageModel, "2:3", "2K");
        }

        /// <summary>
        /// Generate a programming language cheat sheet
        /// </summary>
        public async Task<byte[]> GenerateCheatSheetAsync(string topic, string language)
        {
            var prompt = $"Create a comprehensive {language} programming cheat sheet for '{topic}'. " +
                $"Include syntax examples, common patterns, best practices, and quick reference information. " +
                $"Use a clean, organized layout with code blocks, clear headings, and well-defined sections. " +
                $"Make it printer-friendly and easy to scan quickly. Use monospace fonts for code and professional typography for headings.";

            // Use Pro model with 2K resolution for better text rendering
            return await GenerateImageAsync(prompt, _proImageModel, "4:3", "2K");
        }

        /// <summary>
        /// Core method to generate images using Gemini API
        /// FIXED: Proper implementation based on official Gemini documentation
        /// </summary>
        private async Task<byte[]> GenerateImageAsync(string prompt, string model, string aspectRatio, string imageSize)
        {
            try
            {
                _logger.LogInformation("=== GENERATING IMAGE ===");
                _logger.LogInformation("Model: {Model}", model);
                _logger.LogInformation("Aspect Ratio: {AspectRatio}", aspectRatio);
                _logger.LogInformation("Image Size: {ImageSize}", imageSize);
                _logger.LogInformation("Prompt: {Prompt}", prompt.Substring(0, Math.Min(100, prompt.Length)) + "...");

                // Gemini API endpoint for generateContent
                var apiUrl = $"{GEMINI_API_BASE}/{model}:generateContent?key={_apiKey}";

                // Build request payload according to official Gemini API format
                // According to docs, we just send contents with text, no special responseModalities needed
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseModalities = new[] { "IMAGE" },  // Request IMAGE output
                        imageConfig = new
                        {
                            aspectRatio = aspectRatio,  // e.g., "1:1", "16:9", "4:3"
                            imageSize = imageSize  // Must be "1K", "2K", or "4K" (uppercase K)
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                _logger.LogDebug("API URL: {Url}", apiUrl.Replace(_apiKey, "***"));
                _logger.LogDebug("Request Body: {Body}", jsonContent);

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send request
                var response = await _httpClient.PostAsync(apiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("=== GEMINI API ERROR ===");
                    _logger.LogError("Status Code: {StatusCode}", response.StatusCode);
                    _logger.LogError("Response: {Response}", responseContent);
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {responseContent}");
                }

                _logger.LogInformation("Received successful response from Gemini API");
                _logger.LogDebug("Response length: {Length} characters", responseContent.Length);

                // Parse response
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // Log the full response structure for debugging
                _logger.LogDebug("Full Response: {Response}", responseContent);

                // Extract image data from response
                // Response format: { candidates: [{ content: { parts: [{ inlineData: { mimeType, data } }] } }] }
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    _logger.LogInformation("Found {Count} candidate(s)", candidates.GetArrayLength());

                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content))
                    {
                        _logger.LogInformation("Found content in first candidate");

                        if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            _logger.LogInformation("Found {Count} part(s)", parts.GetArrayLength());

                            // Iterate through parts to find the image
                            foreach (var part in parts.EnumerateArray())
                            {
                                // Try inlineData (camelCase)
                                if (part.TryGetProperty("inlineData", out var inlineData))
                                {
                                    _logger.LogInformation("Found inlineData in part");

                                    if (inlineData.TryGetProperty("data", out var base64Data))
                                    {
                                        var base64String = base64Data.GetString();
                                        if (!string.IsNullOrEmpty(base64String))
                                        {
                                            _logger.LogInformation("Successfully extracted base64 image data ({Length} chars)",
                                                base64String.Length);
                                            return Convert.FromBase64String(base64String);
                                        }
                                    }
                                }
                                // Also try inline_data (snake_case) just in case
                                else if (part.TryGetProperty("inline_data", out var inlineDataSnake))
                                {
                                    _logger.LogInformation("Found inline_data (snake_case) in part");

                                    if (inlineDataSnake.TryGetProperty("data", out var base64Data))
                                    {
                                        var base64String = base64Data.GetString();
                                        if (!string.IsNullOrEmpty(base64String))
                                        {
                                            _logger.LogInformation("Successfully extracted base64 image data from snake_case ({Length} chars)",
                                                base64String.Length);
                                            return Convert.FromBase64String(base64String);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Part does not contain inlineData or inline_data. Part keys: {Keys}",
                                        string.Join(", ", part.EnumerateObject().Select(p => p.Name)));
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Content has no parts or parts is empty");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("First candidate has no content property");
                    }
                }
                else
                {
                    _logger.LogWarning("Response has no candidates or candidates is empty");
                }

                _logger.LogError("No image data found in response after exhaustive search");
                throw new Exception($"No image data found in Gemini API response. The model may have returned only text. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image with Gemini API");
                throw;
            }
        }
    }
}