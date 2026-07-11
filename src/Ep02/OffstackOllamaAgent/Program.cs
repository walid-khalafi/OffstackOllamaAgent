using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ep02.OffstackOllamaAgent;
using Microsoft.Extensions.AI;

class Program
{
    static async Task Main(string[] args)
    {
        // Force terminal runtime output encoding to support UTF8 characters cleanly
        Console.OutputEncoding = Encoding.UTF8;

        // Display the application branding banner
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("====================================================");
        Console.WriteLine("     OFFSTACK - AUTONOMOUS AI AGENT RUNTIME        ");
        Console.WriteLine("====================================================");
        Console.ResetColor();

        string ollamaUrl = "http://localhost:11434";
        string modelId = "llama3.1:8b";

        Console.WriteLine($"\n[System] Connecting to Ollama endpoint at {ollamaUrl}...");

        // Wrap the base client into a FunctionInvocation processing layer.
        // This builder middleware automatically handles the Tool Request -> C# Execute -> LLM Response loop.
        IChatClient chatClient = new OllamaChatClient(new Uri(ollamaUrl), modelId)
            .AsBuilder()
            .UseFunctionInvocation() 
            .Build();

        // Seed initial operational parameters and behavior context via System prompt instructions
        var chatHistory = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are an elite Infrastructure Agent inside the Offstack ecosystem. You can use available local tools to execute network commands when asked.")
        };

        // Initialize local execution platform components
        var networkTools = new NetworkUtilityTools();

        // Register custom native C# automation hooks inside execution configuration options
        // FIX: Included the newly added port-scanning methods from the updated utility class
        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(networkTools.PingHost),
                AIFunctionFactory.Create(networkTools.ConnectViaSSH),
                AIFunctionFactory.Create(networkTools.InitiateRDPSession),
                AIFunctionFactory.Create(networkTools.ScanPorts),
                AIFunctionFactory.Create(networkTools.ScanPortsInRange)
            ]
        };

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n Local AI Agent Platform [{modelId}] is online.");
        Console.WriteLine("Ask the agent to manage network infrastructure. (Type 'exit' to shut down)");
        Console.ResetColor();

        // Main execution processing thread loop
        while (true)
        {
            Console.Write("\nUser > ");
            string? userInput = Console.ReadLine();

            // Handle system termination triggers smoothly
            if (string.IsNullOrEmpty(userInput) || userInput.Trim().ToLower() == "exit")
            {
                Console.WriteLine("\n[System] Shutting down runtime environment. Goodbye!");
                break;
            }

            // Append conversational trace state records
            chatHistory.Add(new ChatMessage(ChatRole.User, userInput));

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"\nOffstackAgent > ");
            Console.ResetColor();

            try
            {
                // Stream the token chunk components from the execution agent instance
                var responseStream = chatClient.GetStreamingResponseAsync(chatHistory, chatOptions);
                var completeResponse = new StringBuilder();

                await foreach (var update in responseStream)
                {
                    if (update.Text != null)
                    {
                        Console.Write(update.Text);
                        completeResponse.Append(update.Text);
                    }
                }
                Console.WriteLine();

                // Keep model state aligned across execution requests
                if (completeResponse.Length > 0)
                {
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, completeResponse.ToString()));
                }
            }
            catch (Exception ex)
            {
                // Catch underlying operational, socket or network engine layer runtime exceptions
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Runtime Error] Agent pipeline execution failure.");
                Console.WriteLine($"Details: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}