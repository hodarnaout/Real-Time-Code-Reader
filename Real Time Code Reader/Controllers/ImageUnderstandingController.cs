using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Real_Time_Code_Reader.Controllers
{
    [ApiController]
    [Route("api/image")]
    public class ImageUnderstandingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ImageUnderstandingController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("extract-code")]
        public async Task<IActionResult> ExtractCode()
        {
            try
            {
                var imageBytes = await ReadImageFromRequest();
                var base64Image = Convert.ToBase64String(imageBytes);

                var analysis = await AnalyzeImageWithGemini(
                    base64Image,
                    @"You are an expert code extraction AI. Analyze this image and extract ALL code visible in it.

IMPORTANT RULES:
1. Extract the COMPLETE code exactly as shown - do NOT modify, summarize, or truncate
2. Preserve all formatting, indentation, and syntax
3. If there are multiple code snippets, extract them all
4. Include comments and documentation
5. Return ONLY the extracted code, no explanations or markdown formatting
6. If no code is found, return: 'No code detected in this image'

Extract the code now:"
                );

                return Ok(new { code = analysis });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExtractCode: {ex}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage()
        {
            try
            {
                var imageBytes = await ReadImageFromRequest();
                var base64Image = Convert.ToBase64String(imageBytes);

                var analysisJson = await AnalyzeImageWithGemini(
                    base64Image,
                    @"Analyze this image in detail and provide insights in JSON format:

{
  ""description"": ""A comprehensive description of what you see in the image"",
  ""keyPoints"": [
    ""First key observation or insight"",
    ""Second key observation or insight"",
    ""Third key observation or insight"",
    ""Additional important details""
  ],
  ""technicalDetails"": [
    ""Technical aspect 1"",
    ""Technical aspect 2"",
    ""Technical aspect 3""
  ],
  ""suggestions"": [
    ""Suggestion for improvement or best practice"",
    ""Another actionable recommendation"",
    ""Additional insight or tip""
  ]
}

If this is a code-related image, focus on:
- Programming language and framework
- Code structure and patterns
- Potential issues or improvements
- Best practices demonstrated or missing

If this is a diagram or flowchart, focus on:
- Type of diagram (flowchart, UML, architecture, etc.)
- Components and relationships
- Purpose and use case
- Clarity and completeness

Provide at least 3-5 items in each array."
                );

                var analysis = ParseJsonResponse(analysisJson);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AnalyzeImage: {ex}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("generate-diagram")]
        public async Task<IActionResult> GenerateDiagram([FromBody] DiagramRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Code))
                {
                    return BadRequest(new { error = "Code is required" });
                }

                // Generate diagram description using Gemini
                var diagramPrompt = $@"Analyze the following code and create a detailed Mermaid diagram specification:

```
{request.Code}
```

Create a Mermaid diagram that best represents this code. Choose the appropriate diagram type:
- Flowchart for procedural logic
- Class diagram for OOP structures
- Sequence diagram for interactions
- State diagram for state machines

Return ONLY the Mermaid diagram code (starting with ```mermaid), no explanations.
Make it comprehensive and well-structured.";

                var mermaidCode = await AnalyzeImageWithGemini(null, diagramPrompt);

                // Clean up the mermaid code
                mermaidCode = mermaidCode.Trim();
                if (mermaidCode.StartsWith("```mermaid"))
                {
                    mermaidCode = mermaidCode.Substring(10);
                }
                if (mermaidCode.EndsWith("```"))
                {
                    mermaidCode = mermaidCode.Substring(0, mermaidCode.Length - 3);
                }
                mermaidCode = mermaidCode.Trim();

                // Generate explanation
                var explanationPrompt = $@"Explain the following code in a clear, educational way:

```
{request.Code}
```

Provide:
1. A brief overview of what the code does
2. Key components and their roles
3. How the different parts work together
4. Important concepts or patterns used

Format your response as clear, structured text.";

                var explanation = await AnalyzeImageWithGemini(null, explanationPrompt);

                return Ok(new
                {
                    mermaidCode = mermaidCode,
                    explanation = explanation,
                    diagramType = DetectDiagramType(mermaidCode)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateDiagram: {ex}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<byte[]> ReadImageFromRequest()
        {
            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private async Task<string> AnalyzeImageWithGemini(string base64Image, string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? _configuration["GeminiSettings:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Gemini API key not configured. Set GEMINI_API_KEY environment variable or add GeminiSettings:ApiKey to appsettings.json");
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = base64Image != null
                            ? new object[]
                            {
                                new { inline_data = new { mime_type = "image/jpeg", data = base64Image } },
                                new { text = prompt }
                            }
                            : new object[]
                            {
                                new { text = prompt }
                            }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key={apiKey}",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObj.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentObj))
                {
                    if (contentObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var textObj))
                        {
                            return textObj.GetString();
                        }
                    }
                }
            }

            throw new Exception("Failed to get a valid response from Gemini API.");
        }

        private object ParseJsonResponse(string jsonResponse)
        {
            try
            {
                var cleanJson = jsonResponse.Trim();
                if (cleanJson.StartsWith("```"))
                {
                    cleanJson = cleanJson.Substring(cleanJson.IndexOf('\n') + 1);
                    cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf("```")).Trim();
                }

                return JsonSerializer.Deserialize<object>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return as plain text
                return new
                {
                    description = jsonResponse,
                    keyPoints = new[] { "Analysis completed" },
                    technicalDetails = new[] { "See description for details" },
                    suggestions = new[] { "Review the analysis above" }
                };
            }
        }

        private string DetectDiagramType(string mermaidCode)
        {
            if (mermaidCode.Contains("graph") || mermaidCode.Contains("flowchart"))
                return "flowchart";
            if (mermaidCode.Contains("classDiagram"))
                return "class";
            if (mermaidCode.Contains("sequenceDiagram"))
                return "sequence";
            if (mermaidCode.Contains("stateDiagram"))
                return "state";
            if (mermaidCode.Contains("erDiagram"))
                return "er";
            return "flowchart";
        }
    }

    public class DiagramRequest
    {
        public string Code { get; set; }
    }
}
