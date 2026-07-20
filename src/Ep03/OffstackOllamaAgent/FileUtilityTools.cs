using System.ComponentModel;
using System.IO;
using System.Text;

/// <summary>
/// Local sandboxed file tools for the agent.
/// All file operations are constrained under the AgentWorkspace directory.
/// </summary>
public class FileUtilityTools
{
    private const string WorkspaceDirectoryName = "AgentWorkspace";

    private static string GetWorkspaceRoot()
    {
        // Keep the sandbox stable per app execution directory.
        var root = AppContext.BaseDirectory;
        var workspaceRoot = Path.Combine(root, WorkspaceDirectoryName);
        Directory.CreateDirectory(workspaceRoot);
        return workspaceRoot;
    }

    private static string ResolveSafePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("fileName must be a non-empty value.", nameof(fileName));
        }

        // Treat slashes uniformly and keep fileName relative.
        var normalized = fileName.Replace('\\', '/').TrimStart('/');
        if (Path.IsPathRooted(normalized))
        {
            throw new ArgumentException("fileName must be a relative path inside the sandbox.", nameof(fileName));
        }

        var workspaceRoot = GetWorkspaceRoot();
        var combinedPath = Path.Combine(workspaceRoot, normalized);

        // Canonicalize and ensure the resolved path stays inside the sandbox.
        var fullPath = Path.GetFullPath(combinedPath, workspaceRoot);
        var fullRoot = Path.GetFullPath(workspaceRoot);

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Access denied: path traversal detected.");
        }

        return fullPath;
    }

    [Description("Reads the complete text content of a file from the AgentWorkspace sandbox.")]
    public string ReadFile(string fileName)
    {
        var safePath = ResolveSafePath(fileName);

        if (!File.Exists(safePath))
        {
            return $"Error: File not found: {fileName}";
        }

        return File.ReadAllText(safePath, Encoding.UTF8);
    }

    [Description("Creates or overwrites a file inside the AgentWorkspace sandbox with the provided text content.")]
    public string WriteFile(string fileName, string content)
    {
        var safePath = ResolveSafePath(fileName);

        var directory = Path.GetDirectoryName(safePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(safePath, content ?? string.Empty, Encoding.UTF8);
        return $"Success: Wrote file: {fileName}";
    }

    [Description("Deletes a file from the AgentWorkspace sandbox. Use with caution.")]
    public string DeleteFile(string fileName)
    {
        var safePath = ResolveSafePath(fileName);

        if (!File.Exists(safePath))
        {
            return $"Error: File not found: {fileName}";
        }

        File.Delete(safePath);
        return $"Success: Deleted file: {fileName}";
    }
}

