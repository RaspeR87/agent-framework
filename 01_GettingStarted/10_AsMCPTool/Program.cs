using Azure.AI.Agents.Persistent;
using Azure.Identity;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

Env.TraversePath().Load();

string endpoint   = Get("AZURE_FOUNDRY_PROJECT_ENDPOINT");
string deployment = Get("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME");

var builder = Host.CreateApplicationBuilder(args);

// log samo na stderr (stdout mora ostati čist za MCP JSON)
builder.Logging.AddConsole(o =>
{
    o.LogToStandardErrorThreshold = LogLevel.Trace;
});

Console.Error.WriteLine("[Main] Creating PersistentAgentsClient…");
var persistentAgentsClient = new PersistentAgentsClient(endpoint, new AzureCliCredential());

// Ustvarimo / dobimo persistent agenta enkrat ob startu
Console.Error.WriteLine("[Main] Creating Foundry agent…");
var agentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
    model: deployment,
    instructions: "You are senior software engineer.",
    name: "Developer Assistant",
    description: "Helps with software development questions.");

AIAgent agent = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);
Console.Error.WriteLine($"[Main] Agent created with id: {agentMetadata.Value.Id}");

// Registriraj AIAgent za DI (DeveloperAssistantTool ga bo dobil v konstruktor)
builder.Services.AddSingleton(agent);

// Registriraj MCP server in *vsa* orodja iz assemblyja (EchoTool, DeveloperAssistantTool, ...)
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");