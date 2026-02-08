using DevMentorAI.API.Models;


namespace DevMentorAI.API.Services
{
    public interface ICodeExecutionService
    {
        Task<Models.CodeExecutionResult> ExecuteCodeAsync(CodeExecutionRequest request);
    }
}