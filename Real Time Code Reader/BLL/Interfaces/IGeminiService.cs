using DevMentorAI.API.Models;

namespace DevMentorAI.API.Services
{
    public interface IGeminiService
    {
        Task<string> AnalyzeCodeAsync(CodeAnalysisRequest request);
        Task<string> AnalyzeScreenshotAsync(ScreenshotRequest request);
        Task<string> GeneratePracticeAsync(PracticeRequest request);
        Task<string> ExplainConceptAsync(ConceptRequest request);
    }
}