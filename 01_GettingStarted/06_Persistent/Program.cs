using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using OpenAI;

Env.TraversePath().Load();

string endpoint   = Get("AZURE_OPENAI_ENDPOINT");
string key        = Get("AZURE_OPENAI_API_KEY");
string deployment = Get("AZURE_OPENAI_DEPLOYMENT_NAME");

// Create the agent
AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(key))
    .GetChatClient(deployment)
    .CreateAIAgent(instructions: "You are senior software engineer.", name: "Developer Assistant");

// Start a new thread for the agent conversation.
AgentThread thread = agent.GetNewThread();

// Run the agent with a new thread.
Console.WriteLine(await agent.RunAsync("Tell me which language is most popular for development.", thread));

// Serialize the thread state to a JsonElement, so it can be stored for later use.
JsonElement serializedThread = thread.Serialize();

// Save the serialized thread to a temporary file (for demonstration purposes).
string tempFilePath = Path.GetTempFileName();
await File.WriteAllTextAsync(tempFilePath, JsonSerializer.Serialize(serializedThread));

// Load the serialized thread from the temporary file (for demonstration purposes).
JsonElement reloadedSerializedThread = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(tempFilePath));

// Deserialize the thread state after loading from storage.
AgentThread resumedThread = agent.DeserializeThread(reloadedSerializedThread);

// Run the agent again with the resumed thread.
Console.WriteLine(await agent.RunAsync("Now tell me which of them to use if I want to build a web application in Microsoft ecosystem.", resumedThread));

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");