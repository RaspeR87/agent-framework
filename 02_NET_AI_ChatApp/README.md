# 1. Prepare AI Chat App (Blazor App) from .NET template "aichatweb" + add Aspire orchestration

dotnet new install Microsoft.Extensions.AI.Templates 
dotnet new aichatweb --Framework net9.0 --provider azureopenai --vector-store local -n ChatApp.Web
dotnet new aspire-apphost -f net9.0 -n ChatApp.AppHost
dotnet new aspire-servicedefaults -f net9.0 -n ChatApp.ServiceDefaults

dotnet sln agent_framework.sln add 02_NET_AI_ChatApp/ChatApp.AppHost/ChatApp.AppHost.csproj
dotnet sln agent_framework.sln add 02_NET_AI_ChatApp/ChatApp.ServiceDefaults/ChatApp.ServiceDefaults.csproj
dotnet sln agent_framework.sln add 02_NET_AI_ChatApp/ChatApp.Web/ChatApp.Web.csproj

dotnet user-secrets init

dotnet user-secrets set "Aspire:Azure:AI:OpenAI:Endpoint" "https://<your-aoai>.openai.azure.com"
dotnet user-secrets set "Aspire:Azure:AI:OpenAI:Key" "<YOUR_KEY>"

dotnet user-secrets set "AzureOpenAI:ChatDeployment" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:EmbeddingDeployment" "text-embedding-3-small"

Modify ChatApp.Web/Program.cs to read this user secrets

# 2. Adding Microsoft Agent Framework

We want more flexibility:
- Better separation of concerns – Moving tool functions out of UI components
- Easier testing – Testing agent behavior independently from the UI
- More sophisticated patterns – Support for complex reasoning and multi-step workflows
- Agent orchestration – Coordinating multiple specialized agents
- Richer telemetry – Better observability into how your AI makes decisions
That’s exactly what Microsoft Agent Framework brings to the table. Let’s see how!

```
 <!-- Add Microsoft Agent Framework packages -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251009.1" />
<PackageReference Include="Microsoft.Agents.AI.Abstractions" Version="1.0.0-preview.251009.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.251009.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.OpenAI" Version="1.0.0-alpha.251009.1" />
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview.251009.1" />
```

The key Agent Framework packages are:
- Microsoft.Agents.AI – Core agent abstractions and implementations
- Microsoft.Agents.AI.Abstractions – Base interfaces and types
- Microsoft.Agents.AI.Hosting – Dependency injection and hosting extensions
- Microsoft.Agents.AI.Hosting.OpenAI – OpenAI-specific hosting support
- Microsoft.Agents.AI.OpenAI – OpenAI integration for agents

## 2.1. Creating a Dedicated Search Functions Service
To promote better separation of concerns and testability, create a new SearchFunctions.cs service that wraps the semantic search functionality:

```
ChatApp.Web.Services.SearchFunctions.cs
```

Why this is important:
- The SearchFunctions class is now a dedicated service that can be injected into the agent
- It’s testable in isolation from the UI
- The [Description] attributes provide metadata that helps the AI understand when and how to use the tool
- The agent can invoke this function automatically when it needs to search for information

## 2.2. Registering the AI Agent in Program.cs
Now, let’s configure the AI agent in Program.cs using the Agent Framework’s hosting extensions:

```
// Register the AI Agent using the Agent Framework
builder.AddAIAgent("ChatAgent", (sp, key) =>
{
    // Get required services
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Configuring AI Agent with key '{Key}' for model '{Model}'", key, "gpt-4o");

    var searchFunctions = sp.GetRequiredService<SearchFunctions>();
    var chatClient = sp.GetRequiredService<IChatClient>();

    // Create and configure the AI agent
    var aiAgent = chatClient.CreateAIAgent(
        name: key,
        instructions: "You are a useful agent that helps users with short and funny answers.",
        description: "An AI agent that helps users with short and funny answers.",
        tools: [AIFunctionFactory.Create(searchFunctions.SearchAsync)]
        )
    .AsBuilder()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment())
    .Build();

    return aiAgent;
});

...

// Register SearchFunctions for DI injection into the agent
builder.Services.AddSingleton<SearchFunctions>();
```

Key points about the agent registration:
- Keyed Service Registration: The agent is registered with the key "ChatAgent" using builder.AddAIAgent().
  This allows you to register multiple agents in the same application.
- Agent Configuration: The agent is created with:
    - A name for identification
    - Instructions (system prompt) that define its personality and behavior
    - A description that explains its purpose
    - Tools that the agent can use (in this case, the SearchAsync function)
- Tool Binding: The AIFunctionFactory.Create() method converts the SearchAsync method into a tool that the 
  agent can invoke. The framework automatically handles:
    - Parameter validation based on the [Description] attributes
    - JSON serialization/deserialization
    - Error handling and retries
- Telemetry: The UseOpenTelemetry() call ensures that all agent interactions are logged and can be observed 
  through Application Insights or other monitoring tools.
- Dependency Injection: The agent factory receives an IServiceProvider, allowing it to resolve dependencies 
  like SearchFunctions and IChatClient.

## 2.3. Updating the Chat Component
Finally, we need to update Chat.razor to use our new AI agent. The changes are pretty straightforward:

Key changes in the code-behind:

Inject the IServiceProvider instead of IChatClient:
```
@inject IServiceProvider ServiceProvider
@using Microsoft.Agents.AI
```

Resolve the agent in OnInitialized():
```
private AIAgent aiAgent = default!;

protected override void OnInitialized()
{
    // Resolve the keyed AI agent registered as "ChatAgent" in Program.cs
    aiAgent = ServiceProvider.GetRequiredKeyedService<AIAgent>("ChatAgent");
    // ... rest of initialization ...
}
```

# 3. Running and Testing the Enhanced Application
## 3.1. Running with .NET Aspire
One of the best parts about using the AI templates is that everything runs through .NET Aspire. This gives you:
- Service discovery between components
- Unified logging and telemetry in the Aspire dashboard
- Health checks for all services
- Easy configuration for all your secrets and settings

Run the app. The Aspire dashboard opens automatically in your browser

[ SLIKA ]

## 3.2. Configuring Azure OpenAI

On first run, you’ll be prompted to configure Azure OpenAI:

Azure Subscription: Select your subscription
Resource Group: Choose existing or create new
Azure OpenAI Resource: Select or provision
Model Deployments: Ensure you have:
A chat model (e.g., gpt-4o)
An embedding model (e.g., text-embedding-3-small)
The configuration will be saved locally and reused for subsequent runs.