using Microsoft.AspNetCore.SignalR;
using DevMentorAI.API.Services;
using DevMentorAI.API.Models;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DevMentorAI.API.Hubs
{
    public class CodeAnalysisHub : Hub
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<CodeAnalysisHub> _logger;
        private readonly ICodeExecutionService _codeExecutionService;

        public CodeAnalysisHub(IGeminiService geminiService, ILogger<CodeAnalysisHub> logger, ICodeExecutionService codeExecutionService)
        {
            _geminiService = geminiService;
            _logger = logger;
            _codeExecutionService = codeExecutionService;
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

        public async Task ExecuteCode(string code, string language)
        {
            try
            {
                _logger.LogInformation($"Executing {language} code");

                await Clients.Caller.SendAsync("ExecutionStatus", new
                {
                    status = "running",
                    message = $"Running your {language} code...",
                    timestamp = DateTime.UtcNow
                });

                var request = new CodeExecutionRequest
                {
                    Code = code,
                    Language = language ?? "python"
                };

                var startTime = DateTime.UtcNow;
                var result = await _codeExecutionService.ExecuteCodeAsync(request);
                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                result.ExecutionTimeMs = executionTime;

                await Clients.Caller.SendAsync("ExecutionComplete", new
                {
                    success = result.Success,
                    output = result.Output,
                    error = result.Error,
                    exitCode = result.ExitCode,
                    language = result.Language,
                    executionTimeMs = result.ExecutionTimeMs,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing code");
                await Clients.Caller.SendAsync("ExecutionError", new
                {
                    error = "Execution failed",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // NEW: Terminal Command Execution Method
        public async Task ExecuteTerminalCommand(string command)
        {
            try
            {
                _logger.LogInformation($"Executing terminal command: {command}");

                string shell;
                string shellArgs;

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    shell = "cmd.exe";
                    shellArgs = $"/c {command}";
                }
                else
                {
                    shell = "/bin/bash";
                    shellArgs = $"-c \"{command}\"";
                }

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = shellArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.OutputDataReceived += async (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        await Clients.Caller.SendAsync("ReceiveTerminalOutput", args.Data);
                    }
                };

                process.ErrorDataReceived += async (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        await Clients.Caller.SendAsync("ReceiveTerminalOutput", args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                await Clients.Caller.SendAsync("TerminalCommandComplete", process.ExitCode);

                _logger.LogInformation($"Command completed with exit code: {process.ExitCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing terminal command");
                await Clients.Caller.SendAsync("ReceiveTerminalOutput", $"Error: {ex.Message}");
                await Clients.Caller.SendAsync("TerminalCommandComplete", -1);
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
