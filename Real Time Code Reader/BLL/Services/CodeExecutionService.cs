using System.Diagnostics;
using System.Text;
using DevMentorAI.API.Models;

namespace DevMentorAI.API.Services
{
    public class CodeExecutionService : ICodeExecutionService
    {
        private readonly ILogger<CodeExecutionService> _logger;
        private readonly string _tempDirectory;

        public CodeExecutionService(ILogger<CodeExecutionService> logger)
        {
            _logger = logger;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "DevMentorAI");

            // Create temp directory if it doesn't exist
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public async Task<CodeExecutionResult> ExecuteCodeAsync(CodeExecutionRequest request)
        {
            var result = new CodeExecutionResult
            {
                Language = request.Language,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation($"🚀 Executing {request.Language} code");

                switch (request.Language.ToLower())
                {
                    case "python":
                        result = await ExecutePythonAsync(request.Code);
                        break;
                    case "javascript":
                        result = await ExecuteJavaScriptAsync(request.Code);
                        break;
                    case "csharp":
                    case "c#":
                        result = await ExecuteCSharpAsync(request.Code);
                        break;
                    case "java":
                        result = await ExecuteJavaAsync(request.Code);
                        break;
                    case "cpp":
                    case "c++":
                        result = await ExecuteCppAsync(request.Code);
                        break;
                    case "go":
                        result = await ExecuteGoAsync(request.Code);
                        break;
                    case "rust":
                        result = await ExecuteRustAsync(request.Code);
                        break;
                    case "ruby":
                        result = await ExecuteRubyAsync(request.Code);
                        break;
                    case "php":
                        result = await ExecutePhpAsync(request.Code);
                        break;
                    default:
                        result.Success = false;
                        result.Error = $"Language '{request.Language}' is not supported for execution yet. Supported: Python, JavaScript, C#, Java, C++, Go, Rust, Ruby, PHP";
                        break;
                }

                _logger.LogInformation($"✅ Execution completed: Success={result.Success}, ExitCode={result.ExitCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error executing code");
                result.Success = false;
                result.Error = $"Execution error: {ex.Message}";
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecutePythonAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "python" };
            var fileName = $"script_{Guid.NewGuid()}.py";
            var filePath = Path.Combine(_tempDirectory, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(processInfo, "Python");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Python execution failed: {ex.Message}. Make sure Python is installed and in PATH.";
            }
            finally
            {
                CleanupFile(filePath);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteJavaScriptAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "javascript" };
            var fileName = $"script_{Guid.NewGuid()}.js";
            var filePath = Path.Combine(_tempDirectory, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(processInfo, "Node.js");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Node.js execution failed: {ex.Message}. Make sure Node.js is installed and in PATH.";
            }
            finally
            {
                CleanupFile(filePath);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteCSharpAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "csharp" };
            var fileName = $"script_{Guid.NewGuid()}.csx";
            var filePath = Path.Combine(_tempDirectory, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"script \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(processInfo, "C#");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"C# execution failed: {ex.Message}. Make sure dotnet-script is installed (dotnet tool install -g dotnet-script).";
            }
            finally
            {
                CleanupFile(filePath);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteJavaAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "java" };

            // Extract class name from code (simplified - looks for "public class ClassName")
            var className = ExtractJavaClassName(code);
            var fileName = $"{className}.java";
            var filePath = Path.Combine(_tempDirectory, fileName);
            var classFilePath = Path.Combine(_tempDirectory, $"{className}.class");

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                // Compile Java code
                var compileInfo = new ProcessStartInfo
                {
                    FileName = "javac",
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _tempDirectory
                };

                var compileResult = await ExecuteProcessAsync(compileInfo, "Java Compiler");

                if (!compileResult.Success)
                {
                    result.Success = false;
                    result.Error = $"Compilation failed:\n{compileResult.Error}";
                    result.ExitCode = compileResult.ExitCode;
                    return result;
                }

                // Run compiled Java class
                var runInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = className,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _tempDirectory
                };

                result = await ExecuteProcessAsync(runInfo, "Java");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Java execution failed: {ex.Message}. Make sure JDK is installed and in PATH.";
            }
            finally
            {
                CleanupFile(filePath);
                CleanupFile(classFilePath);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteCppAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "cpp" };
            var sourceFile = Path.Combine(_tempDirectory, $"program_{Guid.NewGuid()}.cpp");
            var exeFile = Path.Combine(_tempDirectory, $"program_{Guid.NewGuid()}.exe");

            try
            {
                await File.WriteAllTextAsync(sourceFile, code);

                // Compile C++ code
                var compileInfo = new ProcessStartInfo
                {
                    FileName = "g++",
                    Arguments = $"\"{sourceFile}\" -o \"{exeFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var compileResult = await ExecuteProcessAsync(compileInfo, "C++ Compiler");

                if (!compileResult.Success)
                {
                    result.Success = false;
                    result.Error = $"Compilation failed:\n{compileResult.Error}";
                    result.ExitCode = compileResult.ExitCode;
                    return result;
                }

                // Run compiled executable
                var runInfo = new ProcessStartInfo
                {
                    FileName = exeFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(runInfo, "C++");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"C++ execution failed: {ex.Message}. Make sure g++ is installed and in PATH.";
            }
            finally
            {
                CleanupFile(sourceFile);
                CleanupFile(exeFile);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteGoAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "go" };
            var fileName = $"main_{Guid.NewGuid()}.go";
            var filePath = Path.Combine(_tempDirectory, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "go",
                    Arguments = $"run \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(processInfo, "Go");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Go execution failed: {ex.Message}. Make sure Go is installed and in PATH.";
            }
            finally
            {
                CleanupFile(filePath);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteRustAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "rust" };
            var sourceFile = Path.Combine(_tempDirectory, $"main_{Guid.NewGuid()}.rs");
            var exeFile = Path.Combine(_tempDirectory, $"program_{Guid.NewGuid()}.exe");

            try
            {
                await File.WriteAllTextAsync(sourceFile, code);

                // Compile Rust code
                var compileInfo = new ProcessStartInfo
                {
                    FileName = "rustc",
                    Arguments = $"\"{sourceFile}\" -o \"{exeFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var compileResult = await ExecuteProcessAsync(compileInfo, "Rust Compiler");

                if (!compileResult.Success)
                {
                    result.Success = false;
                    result.Error = $"Compilation failed:\n{compileResult.Error}";
                    result.ExitCode = compileResult.ExitCode;
                    return result;
                }

                // Run compiled executable
                var runInfo = new ProcessStartInfo
                {
                    FileName = exeFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(runInfo, "Rust");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Rust execution failed: {ex.Message}. Make sure Rust is installed and in PATH.";
            }
            finally
            {
                CleanupFile(sourceFile);
                CleanupFile(exeFile);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecuteRubyAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "ruby" };
            var fileName = $"script_{Guid.NewGuid()}.rb";
            var filePath = Path.Combine(_tempDirectory, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ruby",
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(processInfo, "Ruby");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Ruby execution failed: {ex.Message}. Make sure Ruby is installed and in PATH.";
            }
            finally
            {
                CleanupFile(filePath);
            }

            return result;
        }

        private async Task<CodeExecutionResult> ExecutePhpAsync(string code)
        {
            var result = new CodeExecutionResult { Language = "php" };
            var fileName = $"script_{Guid.NewGuid()}.php";
            var filePath = Path.Combine(_tempDirectory, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, code);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "php",
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                result = await ExecuteProcessAsync(processInfo, "PHP");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"PHP execution failed: {ex.Message}. Make sure PHP is installed and in PATH.";
            }
            finally
            {
                CleanupFile(filePath);
            }

            return result;
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        private async Task<CodeExecutionResult> ExecuteProcessAsync(ProcessStartInfo processInfo, string languageName)
        {
            var result = new CodeExecutionResult { Language = languageName };

            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Timeout after 10 seconds
            var completed = await Task.Run(() => process.WaitForExit(10000));

            if (!completed)
            {
                try { process.Kill(); } catch { }
                result.Success = false;
                result.Error = "⏱️ Execution timed out (10 seconds limit)";
                return result;
            }

            result.Output = outputBuilder.ToString().TrimEnd();
            result.Error = errorBuilder.ToString().TrimEnd();
            result.ExitCode = process.ExitCode;
            result.Success = process.ExitCode == 0;

            return result;
        }

        private string ExtractJavaClassName(string code)
        {
            // Simple regex to extract class name from "public class ClassName"
            var match = System.Text.RegularExpressions.Regex.Match(code, @"public\s+class\s+(\w+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Fallback to default name
            return "MyFirstProgram";
        }

        private void CleanupFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogDebug($"🗑️ Cleaned up: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ Failed to cleanup {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
        }
    }
}