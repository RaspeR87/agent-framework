using System.ComponentModel;
using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

Env.TraversePath().Load();

string endpoint   = Get("AZURE_OPENAI_ENDPOINT");
string key        = Get("AZURE_OPENAI_API_KEY");
string deployment = Get("AZURE_OPENAI_DEPLOYMENT_NAME");

[Description("Talk about dogs and provide interesting facts, tips, or stories.")]
static string TalkAboutDogs(
    [Description("The dog breed or topic to talk about, e.g., Labrador, German Shepherd, or 'dog training'.")]
    string topic)
{
    return topic.ToLower() switch
    {
        "labrador" => "Labradors are friendly, outgoing, and full of energy. They love swimming and are great family dogs.",
        "german shepherd" => "German Shepherds are intelligent, loyal, and often used as working dogs in police or rescue operations.",
        "poodle" => "Poodles are smart and hypoallergenic dogs. They come in different sizes and are excellent at learning tricks.",
        "dog training" => "Consistency, patience, and positive reinforcement are key when training dogs. Always reward good behavior!",
        _ => $"Dogs are amazing companions! Here's something about {topic}: they make life better with their loyalty and love 🐶"
    };
}

// Create the chat client and agent, and provide the function tool to the agent.
AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(key))
    .GetChatClient(deployment)
    .CreateAIAgent(instructions: "You are a funny assistant who loves dogs.", tools: [AIFunctionFactory.Create(TalkAboutDogs)]);

// Non-streaming agent interaction with function tools.
Console.WriteLine(await agent.RunAsync("Tell me something about Huskies"));

// Streaming agent interaction with function tools.
await foreach (var update in agent.RunStreamingAsync("Tell me something about Huskies"))
{
    Console.WriteLine(update);
}

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");