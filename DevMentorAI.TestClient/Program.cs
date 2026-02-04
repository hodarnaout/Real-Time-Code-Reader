using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("🤖 DevMentor AI - Real-Time Test Client");
Console.WriteLine("==========================================\n");

// Build SignalR connection
var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7132/hubs/code-analysis")
    .WithAutomaticReconnect()
    .Build();

// Listen for connection confirmation
connection.On<object>("Connected", (data) =>
{
    Console.WriteLine("✅ Connected to DevMentor AI!");
    Console.WriteLine($"   Connection ID: {data}\n");
});

// Listen for analysis status updates
connection.On<object>("AnalysisStatus", (data) =>
{
    Console.WriteLine($"⏳ Status: {data}\n");
});

// Listen for completed analysis
connection.On<object>("AnalysisComplete", (data) =>
{
    Console.WriteLine("✅ ANALYSIS COMPLETE!");
    Console.WriteLine("==========================================");

    // Parse the response
    var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
    Console.WriteLine("==========================================\n");
});

// Listen for errors
connection.On<object>("AnalysisError", (error) =>
{
    Console.WriteLine($"❌ Error: {error}\n");
});

try
{
    // Start connection
    Console.WriteLine("🔌 Connecting to API...");
    await connection.StartAsync();
    Console.WriteLine("✅ Connection established!\n");

    // Test 1: Analyze simple Python code
    Console.WriteLine("📝 TEST 1: Analyzing Python code...\n");

    var testCode = @"
def calculate_average(numbers):
    total = 0
    for num in numbers:
        total = total + num
    return total / len(numbers)

result = calculate_average([1, 2, 3, 4, 5])
print(result)
";

    await connection.InvokeAsync("AnalyzeCode", testCode, "C#", "beginner");

    // Wait for response
    Console.WriteLine("⏳ Waiting for AI analysis...\n");
    await Task.Delay(10000); // Wait 10 seconds for response

    // Test 2: Explain a concept
    Console.WriteLine("\n📝 TEST 2: Asking for concept explanation...\n");
    await connection.InvokeAsync("ExplainConcept", "recursion", "", "beginner");

    await Task.Delay(10000); // Wait 10 seconds

    // Test 3: Generate practice problem
    Console.WriteLine("\n📝 TEST 3: Generating practice problem...\n");
    await connection.InvokeAsync("GeneratePractice", "loops", "beginner", "python");

    await Task.Delay(10000); // Wait 10 seconds

    Console.WriteLine("\n\n✅ ALL TESTS COMPLETE!");
    Console.WriteLine("Press any key to disconnect...");
    Console.ReadKey();

    // Close connection
    await connection.StopAsync();
    Console.WriteLine("\n👋 Disconnected");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR: {ex.Message}");
    Console.WriteLine("\nMake sure the API is running (press F5 in DevMentorAI.API project)");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();