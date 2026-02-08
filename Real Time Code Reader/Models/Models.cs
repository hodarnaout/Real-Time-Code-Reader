namespace DevMentorAI.API.Models
{
    // ============================================
    // REQUEST MODELS
    // ============================================

    public class CodeAnalysisRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = "C#";
        public string StudentLevel { get; set; } = "beginner";
    }

    public class ScreenshotRequest
    {
        public IFormFile Screenshot { get; set; } = null!;
        public string StudentLevel { get; set; } = "beginner";
        public string Question { get; set; } = string.Empty;
    }

    public class PracticeRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "beginner";
        public string Language { get; set; } = "C#";
    }

    public class ConceptRequest
    {
        public string Concept { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentLevel { get; set; } = "beginner";
    }

    // ============================================
    // RESPONSE MODELS
    // ============================================

    public class CodeAnalysisResponse
    {
        public string Analysis { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int CodeLength { get; set; }
        public string Language { get; set; } = string.Empty;
    }

    public class StatusUpdate
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    public class CodeExecutionRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = "python";
    }

    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public string Language { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public double ExecutionTimeMs { get; set; }
    }
}