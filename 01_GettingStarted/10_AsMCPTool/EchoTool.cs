using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo([Description("Message to echo back")] string message)
        => $"Echo from C#: {message}";
}
