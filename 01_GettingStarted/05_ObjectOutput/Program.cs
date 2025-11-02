using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

Env.TraversePath().Load();

string endpoint   = Get("AZURE_OPENAI_ENDPOINT");
string key        = Get("AZURE_OPENAI_API_KEY");
string deployment = Get("AZURE_OPENAI_DEPLOYMENT_NAME");

// Create the chat client and agent, and provide the function tool to the agent.
ChatClient chatClient = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(key))
    .GetChatClient(deployment);

// Create the ChatClientAgent with the specified name and instructions.
ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions(name: "HelpfulAssistant", instructions: "You are a helpful assistant."));

// Set DogInfo as the type parameter of RunAsync method to specify the expected structured output from the agent and invoke the agent with some unstructured input.
AgentRunResponse<DogInfo> response = await agent.RunAsync<DogInfo>("Please provide information about a Husky.");

// Access the structured output via the Result property of the agent response.
Console.WriteLine("Assistant Output:");
Console.WriteLine($"Name: {response.Result.Name}");
Console.WriteLine($"Eye Color: {response.Result.EyeColor}");
Console.WriteLine($"Fur Color: {response.Result.FurColor}");

// Create the ChatClientAgent with the specified name, instructions, and expected structured output the agent should produce.
ChatClientAgent agentWithPersonInfo = chatClient.CreateAIAgent(new ChatClientAgentOptions(name: "HelpfulAssistant", instructions: "You are a helpful assistant.")
{
    ChatOptions = new()
    {
        ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<DogInfo>()
    }
});

// Invoke the agent with some unstructured input while streaming, to extract the structured information from.
var updates = agentWithPersonInfo.RunStreamingAsync("Please provide information about a Husky.");

// Assemble all the parts of the streamed output, since we can only deserialize once we have the full json,
// then deserialize the response into the DogInfo class.
DogInfo dogInfo = (await updates.ToAgentRunResponseAsync()).Deserialize<DogInfo>(JsonSerializerOptions.Web);

Console.WriteLine("Assistant Output:");
Console.WriteLine($"Name: {dogInfo.Name}");
Console.WriteLine($"Eye Color: {dogInfo.EyeColor}");
Console.WriteLine($"Fur Color: {dogInfo.FurColor}");

static string Get(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is not set.");

/// <summary>
/// Represents information about a dog
/// </summary>
[Description("Information about a dog including its name, eye color and color of fur.")]
public class DogInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("eye_color")]
    public string? EyeColor { get; set; }

    [JsonPropertyName("fur_color")]
    public string? FurColor { get; set; }
}