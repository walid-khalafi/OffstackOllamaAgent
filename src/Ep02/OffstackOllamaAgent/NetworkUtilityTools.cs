using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics; // ProcessStartInfo / Process

namespace Ep02.OffstackOllamaAgent
{
    public class NetworkUtilityTools
    {
        private static string FormatPingReply(string host, IPStatus status, long? roundtripMs)
        {
            var sb = new StringBuilder();
            sb.Append($"Network Reply from {host} : bytes=32");
            if (roundtripMs.HasValue)
                sb.Append($" time={roundtripMs.Value}ms");

            sb.Append(status == IPStatus.Success ? " status=SUCCESS" : $" status=FAILURE({status})");
            return sb.ToString();
        }

        // Returns: (connected/open?, elapsedMs, error)
        private static (bool connected, long elapsedMs, string error) TryTcpConnect(string host, int port, int timeoutMs = 3000)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var completed = connectTask.Wait(timeoutMs);

                if (!completed)
                    return (false, sw.ElapsedMilliseconds, $"timeout after {timeoutMs}ms");

                connectTask.GetAwaiter().GetResult();
                return (true, sw.ElapsedMilliseconds, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, sw.ElapsedMilliseconds, $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string FormatPortResult(string host, int port, bool isOpen, long elapsedMs, string error)
        {
            if (isOpen)
                return $"{host}:{port} open time={elapsedMs}ms";

            if (!string.IsNullOrWhiteSpace(error))
                return $"{host}:{port} closedOrFiltered error={error}";

            return $"{host}:{port} closedOrFiltered";
        }

        [Description("Pings a remote host or server IP address to check its network availability status.")]
        public string PingHost(string hostName)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Agent Action] Executing REAL C# PingHost for target: {hostName}...");
            Console.ResetColor();

            try
            {
                using var ping = new Ping();
                const int timeoutMs = 4000;

                var reply = ping.Send(hostName, timeoutMs);
                return FormatPingReply(
                    hostName,
                    reply.Status,
                    reply.Status == IPStatus.Success ? reply.RoundtripTime : null
                );
            }
            catch (Exception ex)
            {
                return $"Network Reply from {hostName} : bytes=32 status=FAILURE({ex.GetType().Name}: {ex.Message})";
            }
        }

        [Description("Scans a remote host TCP ports for a given range using real TCP connect attempts.")]
        public string ScanPortsInRange(string hostIp, int startPort, int endPort, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(hostIp))
                return "PortScan TCP range: hostIp is empty";

            if (startPort > endPort)
            {
                var tmp = startPort;
                startPort = endPort;
                endPort = tmp;
            }

            startPort = Math.Max(1, startPort);
            endPort = Math.Min(65535, endPort);

            var results = new StringBuilder();
            results.Append($"PortScan TCP range {hostIp}:{startPort}-{endPort} (timeoutMs={timeoutMs})\n");

            int openCount = 0;
            int total = 0;

            for (int port = startPort; port <= endPort; port++)
            {
                total++;

                var (connected, elapsedMs, error) = TryTcpConnect(hostIp, port, timeoutMs);
                if (connected) openCount++;

                if (connected)
                    results.Append($"OPEN {FormatPortResult(hostIp, port, true, elapsedMs, string.Empty)}\n");
                else
                    results.Append($"{FormatPortResult(hostIp, port, false, elapsedMs, error)}\n");
            }

            results.Append($"Summary: openPorts={openCount} totalPorts={total}");
            return results.ToString();
        }

        [Description("Scans a remote host TCP ports for a provided list using real TCP connect attempts.")]
        public string ScanPorts(string hostIp, string portsCsv, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(hostIp))
                return "PortScan TCP list: hostIp is empty";

            if (string.IsNullOrWhiteSpace(portsCsv))
                return $"PortScan TCP list: invalid portsCsv. hostIp={hostIp}";

            var ports = portsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var results = new StringBuilder();
            results.Append($"PortScan TCP list {hostIp} ports=[{portsCsv}] (timeoutMs={timeoutMs})\n");

            int openCount = 0;
            int total = 0;

            foreach (var raw in ports)
            {
                total++;

                var token = raw.Trim();
                if (!int.TryParse(token, out var port))
                {
                    results.Append($"INVALID_PORT '{token}'\n");
                    continue;
                }

                port = Math.Max(1, Math.Min(65535, port));

                var (connected, elapsedMs, error) = TryTcpConnect(hostIp, port, timeoutMs);
                if (connected) openCount++;

                if (connected)
                    results.Append($"OPEN {FormatPortResult(hostIp, port, true, elapsedMs, string.Empty)}\n");
                else
                    results.Append($"{FormatPortResult(hostIp, port, false, elapsedMs, error)}\n");
            }

            results.Append($"Summary: openPorts={openCount} totalPorts={total}");
            return results.ToString();
        }

        [Description("Checks whether a remote Linux host accepts TCP connections on port 22 (SSH).")]
        public string ConnectViaSSH(string hostIp, string username)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Agent Action] Checking REAL SSH reachability (TCP/22) to {hostIp} as user '{username}'...");
            Console.ResetColor();

            var (connected, elapsedMs, error) = TryTcpConnect(hostIp, 22, timeoutMs: 3000);

            return connected
                ? $"SSH port reachable: {hostIp}:22 (username={username}) status=SUCCESS"
                : $"SSH port NOT reachable: {hostIp}:22 (username={username}) status=FAILURE({error})";
        }

        [Description("Checks whether a remote Windows host accepts TCP connections on port 3389 (RDP) and if reachable opens mstsc so the user can connect.")]
        public string InitiateRDPSession(string targetServerIp)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Agent Action] Checking REAL RDP reachability (TCP/3389) to {targetServerIp}...");
            Console.ResetColor();

            var (connected, elapsedMs, error) = TryTcpConnect(targetServerIp, 3389, timeoutMs: 3000);

            if (!connected)
                return $"RDP port NOT reachable: {targetServerIp}:3389 status=FAILURE({error})";

            try
            {
                // Launch mstsc only as a convenience for the human user.
                // The tool already proved reachability via TCP/3389.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "mstsc",
                    Arguments = $"/v:{targetServerIp}",
                    UseShellExecute = true
                });

                return $"RDP port reachable: {targetServerIp}:3389 status=SUCCESS (mstsc launched)";
            }
            catch (Exception ex)
            {
                return $"RDP port reachable: {targetServerIp}:3389 status=SUCCESS (but failed to launch mstsc: {ex.GetType().Name}: {ex.Message})";
            }
        }
    }
}
