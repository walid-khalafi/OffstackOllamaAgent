using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

/// <summary>
/// Human-approval middleware that wraps an existing IChatClient and intercepts
/// destructive tool usage (e.g., DeleteFile) before the agent is allowed to execute them.
/// 
/// This middleware does NOT inherit from DelegatingChatClient because the latest
/// Microsoft.Extensions.AI package changed its internal structure. Instead, we implement
/// IChatClient directly and delegate all calls to the wrapped inner client.
/// 
/// The middleware inspects ChatOptions.Tools before each request and prompts the user
/// for confirmation when a destructive action is detected.
/// </summary>
public sealed class HumanApprovalMiddleware : IChatClient
{
    // Tools considered destructive and requiring human approval
    private static readonly string[] DestructiveToolNames = ["DeleteFile"];

    // The underlying chat client that performs actual LLM operations
    private readonly IChatClient _inner;

    public HumanApprovalMiddleware(IChatClient inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    // -------------------------------------------------------------------------
    //  NON-STREAMING RESPONSE PIPELINE
    // -------------------------------------------------------------------------

    /// <summary>
    /// Intercepts non-streaming chat requests and applies human approval gating
    /// before delegating execution to the underlying chat client.
    /// </summary>
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatHistory,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var historyList = chatHistory?.ToList() ?? new List<ChatMessage>();

        // Apply human approval logic if tools are present
        if (options?.Tools is { Count: > 0 })
        {
            if (options.Tools.Any(t => t.Name == "DeleteFile"))
            {
                FilterDestructiveTools(options.Tools, historyList);
            }
    
        }

        return _inner.GetResponseAsync(historyList, options, cancellationToken);
    }

    // -------------------------------------------------------------------------
    //  STREAMING RESPONSE PIPELINE
    // -------------------------------------------------------------------------

    /// <summary>
    /// Intercepts streaming chat requests and applies human approval gating
    /// before delegating execution to the underlying chat client.
    /// </summary>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatHistory,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var historyList = chatHistory?.ToList() ?? new List<ChatMessage>();

        // Apply human approval logic if tools are present
        if (options?.Tools is { Count: > 0 })
        {
            FilterDestructiveTools(options.Tools, historyList);
        }

        return _inner.GetStreamingResponseAsync(historyList, options, cancellationToken);
    }

    // -------------------------------------------------------------------------
    //  SERVICE RESOLUTION (Required by IChatClient)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Delegates service resolution to the underlying chat client.
    /// </summary>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return (_inner as IChatClient)?.GetService(serviceType, serviceKey);
    }

    // -------------------------------------------------------------------------
    //  HUMAN APPROVAL LOGIC
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether the current request intends to use destructive tools.
    /// If so, prompts the user for approval and removes the tool if denied.
    /// </summary>
    private static void FilterDestructiveTools(
        IList<AITool> tools,
        IReadOnlyList<ChatMessage> chatHistory)
    {
        // No destructive tools present → nothing to do
        if (!tools.Any(t => DestructiveToolNames.Contains(t.Name, StringComparer.OrdinalIgnoreCase)))
            return;

        // Extract the latest user message
        var latestUserText = chatHistory
            .LastOrDefault(m => m.Role == ChatRole.User)?.Text;

        // If the user did not express destructive intent, skip approval
        if (!ContainsDestructiveIntent(latestUserText))
            return;

        // Prompt the user for approval
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("Do you approve this dangerous file deletion action? (Y/N): ");
        Console.ResetColor();

        var response = Console.ReadLine();
        var approved = string.Equals(response?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);

        // If not approved → remove destructive tools from the execution pipeline
        if (!approved)
        {
            for (int i = tools.Count - 1; i >= 0; i--)
            {
                if (DestructiveToolNames.Contains(tools[i].Name, StringComparer.OrdinalIgnoreCase))
                {
                    tools.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether the user's latest message indicates destructive intent.
    /// </summary>
    private static bool ContainsDestructiveIntent(string? userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return false;

        return userPrompt.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
               userPrompt.Contains("remove", StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    //  DISPOSAL
    // -------------------------------------------------------------------------

    /// <summary>
    /// Disposes the underlying chat client if it supports IDisposable.
    /// </summary>
    public void Dispose()
    {
        (_inner as IDisposable)?.Dispose();
    }
}
