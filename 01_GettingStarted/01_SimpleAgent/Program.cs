using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using OpenAI;

Env.TraversePath().Load();

string endpoint   = Get("AZURE_OPENAI_ENDPOINT");
string key        = Get("AZURE_OPENAI_API_KEY");
string deployment = Get("AZURE_OPENAI_DEPLOYMENT_NAME");

// Use API key instead of AzureCliCredential
AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(key))
    .GetChatClient(deployment)
    .CreateAIAgent(instructions: "Tell me which language is most popular for development.", name: "Developer Assistant");

// Invoke the agent and output the text result.
Console.WriteLine(await agent.RunAsync("Tell me which language is most popular for development."));

// Invoke the agent with streaming support.
await foreach (var update in agent.RunStreamingAsync("Tell me which language is most popular for development."))
{
    Console.WriteLine(update);
}

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");