using DevMentorAI.API.Services;
using DevMentorAI.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Gemini Service
builder.Services.AddSingleton<IGeminiService, GeminiService>();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
});

// Configure CORS (Keep this for external clients if needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:7149")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ============================================
// ADD THESE LINES FOR STATIC FILE SUPPORT
// ============================================
app.UseDefaultFiles();  // Serves index.html as default
app.UseStaticFiles();   // Enables serving files from wwwroot
// ============================================

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<CodeAnalysisHub>("/hubs/code-analysis").RequireCors("AllowAll");
app.MapHub<ChatHub>("/hubs/chat").RequireCors("AllowAll");

app.Logger.LogInformation("🚀 DevMentor AI API starting...");
app.Logger.LogInformation("📡 SignalR hubs available at:");
app.Logger.LogInformation("   - /hubs/code-analysis");
app.Logger.LogInformation("   - /hubs/chat");
app.Logger.LogInformation("🌐 Frontend available at: https://localhost:{port}/");

app.Run();