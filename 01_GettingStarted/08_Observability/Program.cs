using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using OpenAI;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;            // <-- add
using System.Diagnostics;                 // <-- add

Env.TraversePath().Load();

string endpoint   = Get("AZURE_OPENAI_ENDPOINT");
string key        = Get("AZURE_OPENAI_API_KEY");
string deployment = Get("AZURE_OPENAI_DEPLOYMENT_NAME");
string aiConnStr  = Get("APPLICATIONINSIGHTS_CONNECTION_STRING");

// Give your app a stable service identity in AI
const string serviceName = "SeniorDeveloperConsole";
const string serviceVersion = "1.0.0";

// Create TracerProvider with Console + Azure Monitor exporters
string sourceName = Guid.NewGuid().ToString("N");
var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .AddSource(sourceName)
    .AddConsoleExporter();

if (!string.IsNullOrWhiteSpace(aiConnStr))
{
    tracerProviderBuilder.AddAzureMonitorTraceExporter(o => o.ConnectionString = aiConnStr);
}

using var tracerProvider = tracerProviderBuilder.Build();

// Create the agent and enable OpenTelemetry
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))
    .GetChatClient(deployment)
    .CreateAIAgent(instructions: "You are senior software engineer.", name: "Developer Assistant")
    .AsBuilder()
    .UseOpenTelemetry(sourceName: sourceName)   // emits spans to the ActivitySource named sourceName
    .Build();

// Optional: create a manual span so you KNOW something is emitted
using (var activitySource = new ActivitySource(sourceName))
using (var span = activitySource.StartActivity("Warmup"))
{
    span?.SetTag("app.step", "before-first-call");
}

// Invoke the agent and output the text result.
Console.WriteLine(await agent.RunAsync("Tell me which language is most popular for development."));

// Streaming call (also traced)
await foreach (var update in agent.RunStreamingAsync("Tell me which language is most popular for development."))
{
    Console.WriteLine(update);
}

// IMPORTANT: give the exporter time to send
tracerProvider.ForceFlush(); // best-effort flush before dispose

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");
