using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

class Program
{
    static async Task Main(string[] args)
    {
         Console.OutputEncoding = Encoding.UTF8;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("====================================================");
        Console.WriteLine("    OFFSTACK - LOCAL AI LIGHTWEIGHT RUNTIME         ");
        Console.WriteLine("====================================================");
        Console.ResetColor();

        string ollamaUrl = "http://localhost:11434";
        string modelId = "llama3.1:8b";

        Console.WriteLine($"\n[System] Connecting to Ollama at {ollamaUrl}...");
        
   
        IChatClient chatClient = new OllamaChatClient(new Uri(ollamaUrl), modelId);


        var chatHistory = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are an expert system architect inside the Offstack ecosystem. Help with clean code.")
        };


        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n Local AI Core [{modelId}] is online.");
        Console.WriteLine("Type your prompt below. (Type 'exit' to shut down)");
        Console.ResetColor();

         while (true)
        {
            Console.Write("\nUser > ");
            string? userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput) || userInput.Trim().ToLower() == "exit")
            {
                Console.WriteLine("\n[System] Shutting down runtime. Goodbye!");
                break;
            }

            chatHistory.Add(new ChatMessage(ChatRole.User, userInput));

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"\nOffstackArchitect > ");
            Console.ResetColor();

            try
            {
                var responseStream = chatClient.GetStreamingResponseAsync(chatHistory);
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

                chatHistory.Add(new ChatMessage(ChatRole.Assistant, completeResponse.ToString()));
            }
            catch(Exception ex)
            {
                 Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Runtime Error] Pipeline failure.");
                Console.WriteLine($"Details: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}