
using DevMentorAI.API.Services;
using DevMentorAI.API.Hubs;
using Real_Time_Code_Reader.Hubs;
using Real_Time_Code_Reader.BLL.Services; // Add this using statement

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Gemini Service
builder.Services.AddSingleton<IGeminiService, GeminiService>();
builder.Services.AddSingleton<ICodeExecutionService, CodeExecutionService>();
// Add HttpClient support (add this near the top, after builder creation)
builder.Services.AddHttpClient();

// Register NanoBananaService (add this with your other services)
builder.Services.AddScoped<INanoBananaService, NanoBananaService>();

// Register Nano Banana Service
// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
});

// Register TutorHub as a service
builder.Services.AddTransient<TutorHub>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:7149") // Add your frontend URL here
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

// Enable WebSockets
app.UseWebSockets();

// Static file support
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<CodeAnalysisHub>("/hubs/code-analysis").RequireCors("AllowAll");

// Map the TutorHub WebSocket middleware
app.Map("/tutorHub", app =>
{
    app.UseMiddleware<TutorHub>();
});

app.Logger.LogInformation("🚀 DevMentor AI API starting...");
app.Logger.LogInformation("📡 SignalR hubs available at:");
app.Logger.LogInformation("   - /hubs/code-analysis");
app.Logger.LogInformation("   - /tutorHub (WebSocket)");
app.Logger.LogInformation("🌐 Frontend available at: https://localhost:{port}/");

app.Run();
