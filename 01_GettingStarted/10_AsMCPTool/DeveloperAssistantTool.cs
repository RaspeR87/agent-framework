using System.ComponentModel;
using Microsoft.Agents.AI;
using ModelContextProtocol.Server;

[McpServerToolType]
[Description("Developer assistant tools for software engineering help.")]
public class DeveloperAssistantTool
{
    private readonly AIAgent _agent;

    public DeveloperAssistantTool(AIAgent agent)
    {
        _agent = agent;
    }

    [McpServerTool(Name = "developer_assistant")]
    [Description("Answer software development questions as a senior engineer.")]
    public async Task<string> AskAsync(
        [Description("Your software development question.")] string question)
    {
        Console.Error.WriteLine($"[DeveloperAssistantTool] Received question: {question}");

        try
        {
            var thread = _agent.GetNewThread();

            // 🔴 IMPORTANT: no CancellationToken here – ignore MCP cancellation for now
            var result = await _agent.RunAsync(question, thread);

            var text = result.ToString();
            Console.Error.WriteLine($"[DeveloperAssistantTool] Finished, length={text.Length}");

            return text;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[DeveloperAssistantTool] ERROR:");
            Console.Error.WriteLine(ex.ToString());

            // Return the error back to the MCP client so you see it in Inspector
            return "ERROR from DeveloperAssistantTool:\n" + ex;
        }
    }
}
