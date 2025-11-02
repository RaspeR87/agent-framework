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

// Invoke the agent with a multi-turn conversation, where the context is preserved in the thread object.
AgentThread thread = agent.GetNewThread();
Console.WriteLine(await agent.RunAsync("Tell me which language is most popular for development.", thread));
Console.WriteLine(await agent.RunAsync("Now tell me which of them to use if I want to build a web application in Microsoft ecosystem.", thread));

// Invoke the agent with a multi-turn conversation and streaming, where the context is preserved in the thread object.
thread = agent.GetNewThread();
await foreach (var update in agent.RunStreamingAsync("Tell me which language is most popular for development.", thread))
{
    Console.WriteLine(update);
}
await foreach (var update in agent.RunStreamingAsync("Now tell me which of them to use if I want to build a web application in Microsoft ecosystem.", thread))
{
    Console.WriteLine(update);
}

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");