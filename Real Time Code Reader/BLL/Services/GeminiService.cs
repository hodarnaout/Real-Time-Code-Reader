using Mscc.GenerativeAI;
using DevMentorAI.API.Models;

namespace DevMentorAI.API.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _flashModel;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _apiKey = configuration["GeminiSettings:ApiKey"]
                ?? throw new ArgumentNullException("Gemini API Key not configured");
            _model = configuration["GeminiSettings:Model"]
                ?? "gemini-2.0-flash-thinking-exp-01-21";
            _flashModel = configuration["GeminiSettings:FlashModel"]
                ?? "gemini-2.0-flash-exp";
            _logger = logger;
        }

        public async Task<string> AnalyzeCodeAsync(CodeAnalysisRequest request)
        {
            try
            {
                var googleAI = new GoogleAI(apiKey: _apiKey);
                var model = googleAI.GenerativeModel(model: _model);

                var prompt = $@"You are DevMentor AI, an expert coding teacher.

Student Level: {request.StudentLevel}
Language: {request.Language}

Code to analyze:
```{request.Language}
{request.Code}
```

Provide feedback in this format:

✅ **What You Did Well:**
[Be specific and encouraging]

💡 **Areas for Improvement:**
[Explain WHY, not just WHAT to change]

🎓 **Teaching Moment:**
[Explain the underlying concept]

🚀 **Next Step:**
[One specific thing to try next]

Keep it conversational and educational.";

                var response = await model.GenerateContent(prompt);
                return response?.Text ?? "Error: No response generated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code");
                throw;
            }
        }

        public async Task<string> AnalyzeScreenshotAsync(ScreenshotRequest request)
        {
            try
            {
                var googleAI = new GoogleAI(apiKey: _apiKey);
                var model = googleAI.GenerativeModel(model: _flashModel);

                var prompt = $@"You are DevMentor AI, analyzing a coding screenshot.

Student Level: {request.StudentLevel}
Question: {request.Question}

Analyze this screenshot and provide:
1. What I see
2. The issue
3. Step-by-step fix
4. Learning opportunity";

                var response = await model.GenerateContent(prompt);
                return response?.Text ?? "Screenshot analysis not yet implemented";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing screenshot");
                return "Screenshot analysis temporarily unavailable.";
            }
        }

        public async Task<string> GeneratePracticeAsync(PracticeRequest request)
        {
            try
            {
                var googleAI = new GoogleAI(apiKey: _apiKey);
                var model = googleAI.GenerativeModel(model: _model);

                var prompt = $@"Generate a practice problem for {request.Difficulty} students learning {request.Topic} in {request.Language}.

Include:
- Problem title
- Real-world scenario
- Requirements
- Example input/output
- 3 progressive hints
- Starter code
- Test cases

Make it engaging and practical!";

                var response = await model.GenerateContent(prompt);
                return response?.Text ?? "Error: No response generated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating practice");
                throw;
            }
        }

        public async Task<string> ExplainConceptAsync(ConceptRequest request)
        {
            try
            {
                var googleAI = new GoogleAI(apiKey: _apiKey);
                var model = googleAI.GenerativeModel(model: _model);

                var codeSection = !string.IsNullOrEmpty(request.StudentCode)
                    ? $"\nTheir code:\n```\n{request.StudentCode}\n```\n"
                    : "";

                var prompt = $@"Explain {request.Concept} to a {request.StudentLevel} student.
{codeSection}
Include:
- Simple analogy
- How it works
- Common mistakes
- 3 examples (simple, practical, advanced)
- Key takeaway

Be conversational and build intuition!";

                var response = await model.GenerateContent(prompt);
                return response?.Text ?? "Error: No response generated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error explaining concept");
                throw;
            }
        }
    }
}