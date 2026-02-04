using Microsoft.AspNetCore.SignalR;
using DevMentorAI.API.Services;
using DevMentorAI.API.Models;

namespace DevMentorAI.API.Hubs
{
    public class CodeAnalysisHub : Hub
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<CodeAnalysisHub> _logger;

        public CodeAnalysisHub(IGeminiService geminiService, ILogger<CodeAnalysisHub> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");

            await Clients.Caller.SendAsync("Connected", new
            {
                message = "Connected to DevMentor AI",
                connectionId = Context.ConnectionId,
                timestamp = DateTime.UtcNow
            });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task AnalyzeCode(string code, string language, string studentLevel)
        {
            try
            {
                await Clients.Caller.SendAsync("AnalysisStatus", new
                {
                    status = "analyzing",
                    message = "DevMentor is analyzing your code...",
                    timestamp = DateTime.UtcNow
                });

                if (string.IsNullOrWhiteSpace(code) || code.Length < 10)
                {
                    await Clients.Caller.SendAsync("AnalysisStatus", new
                    {
                        status = "waiting",
                        message = "Type more code to get feedback...",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }

                var request = new CodeAnalysisRequest
                {
                    Code = code,
                    Language = language ?? "unknown",
                    StudentLevel = studentLevel ?? "beginner"
                };

                var analysis = await _geminiService.AnalyzeCodeAsync(request);

                await Clients.Caller.SendAsync("AnalysisComplete", new
                {
                    analysis = analysis,
                    language = language,
                    timestamp = DateTime.UtcNow,
                    codeLength = code.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code");
                await Clients.Caller.SendAsync("AnalysisError", new
                {
                    error = "Failed to analyze code",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        public async Task ExplainConcept(string concept, string studentCode, string studentLevel)
        {
            try
            {
                await Clients.Caller.SendAsync("ExplanationStatus", "Explaining concept...");

                var request = new ConceptRequest
                {
                    Concept = concept,
                    StudentCode = studentCode ?? string.Empty,
                    StudentLevel = studentLevel ?? "beginner"
                };

                var explanation = await _geminiService.ExplainConceptAsync(request);

                await Clients.Caller.SendAsync("ExplanationComplete", new
                {
                    concept = concept,
                    explanation = explanation,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error explaining concept");
                await Clients.Caller.SendAsync("ExplanationError", ex.Message);
            }
        }

        public async Task GeneratePractice(string topic, string difficulty, string language)
        {
            try
            {
                await Clients.Caller.SendAsync("PracticeStatus", "Generating practice problem...");

                var request = new PracticeRequest
                {
                    Topic = topic,
                    Difficulty = difficulty ?? "beginner",
                    Language = language ?? "python"
                };

                var problem = await _geminiService.GeneratePracticeAsync(request);

                await Clients.Caller.SendAsync("PracticeComplete", new
                {
                    topic = topic,
                    difficulty = difficulty,
                    problem = problem,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating practice");
                await Clients.Caller.SendAsync("PracticeError", ex.Message);
            }
        }
    }

    public class ChatHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}