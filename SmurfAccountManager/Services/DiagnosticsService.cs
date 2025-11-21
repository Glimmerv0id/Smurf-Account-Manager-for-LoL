using System;
using System.IO;
using System.Linq;
using System.Text;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Services
{
    public class DiagnosticsService
    {
        public static string GenerateDiagnosticReport(AppConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SMURF ACCOUNT MANAGER DIAGNOSTICS ===");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine();

            // Check Riot Games Path
            sb.AppendLine("--- Riot Games Path ---");
            sb.AppendLine($"Configured: {config.RiotGamesPath}");
            sb.AppendLine($"Exists: {Directory.Exists(config.RiotGamesPath)}");
            sb.AppendLine();

            // Check League Client Logs
            sb.AppendLine("--- League Client Logs ---");
            sb.AppendLine($"Path: {config.LeagueClientLogsPath}");
            sb.AppendLine($"Exists: {Directory.Exists(config.LeagueClientLogsPath)}");
            
            if (Directory.Exists(config.LeagueClientLogsPath))
            {
                try
                {
                    var files = Directory.GetFiles(config.LeagueClientLogsPath, "*.*")
                        .Where(f => f.EndsWith(".log") || f.EndsWith(".json"))
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(5)
                        .ToList();

                    sb.AppendLine($"Total log files found: {files.Count}");
                    foreach (var file in files)
                    {
                        var fi = new FileInfo(file);
                        sb.AppendLine($"  - {fi.Name} ({fi.Length} bytes, modified: {fi.LastWriteTime})");
                    }

                    // Try to find accountId in most recent file
                    if (files.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("Searching for accountId, gameName, tagLine in newest file...");
                        var newestFile = files.First();
                        var lines = File.ReadLines(newestFile).Reverse().Take(1000);
                        
                        bool foundAccountId = false;
                        bool foundGameName = false;
                        bool foundTagLine = false;

                        foreach (var line in lines)
                        {
                            if (line.Contains("accountId")) foundAccountId = true;
                            if (line.Contains("gameName")) foundGameName = true;
                            if (line.Contains("tagLine")) foundTagLine = true;

                            if (foundAccountId && foundGameName && foundTagLine)
                            {
                                sb.AppendLine($"✓ Found accountId, gameName, and tagLine in same context");
                                // Show a sample of the line (first 200 chars)
                                var sample = line.Length > 200 ? line.Substring(0, 200) + "..." : line;
                                sb.AppendLine($"  Sample: {sample}");
                                break;
                            }
                        }

                        if (!foundAccountId) sb.AppendLine("✗ 'accountId' not found in recent lines");
                        if (!foundGameName) sb.AppendLine("✗ 'gameName' not found in recent lines");
                        if (!foundTagLine) sb.AppendLine("✗ 'tagLine' not found in recent lines");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Error reading files: {ex.Message}");
                }
            }
            sb.AppendLine();

            // Check Riot Client Logs
            sb.AppendLine("--- Riot Client Logs ---");
            sb.AppendLine($"Path: {config.RiotClientLogsPath}");
            sb.AppendLine($"Exists: {Directory.Exists(config.RiotClientLogsPath)}");
            
            if (Directory.Exists(config.RiotClientLogsPath))
            {
                try
                {
                    var files = Directory.GetFiles(config.RiotClientLogsPath, "*.log")
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(5)
                        .ToList();

                    sb.AppendLine($"Total log files found: {files.Count}");
                    foreach (var file in files)
                    {
                        var fi = new FileInfo(file);
                        sb.AppendLine($"  - {fi.Name} ({fi.Length} bytes, modified: {fi.LastWriteTime})");
                    }

                    // Try to find punishment events
                    if (files.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("Searching for punishment events in newest file...");
                        var newestFile = files.First();
                        var content = File.ReadAllText(newestFile);
                        
                        bool foundLeaverBusted = content.Contains("LEAVER_BUSTED");
                        bool foundLeaverBusterLockout = content.Contains("LEAVER_BUSTER_QUEUE_LOCKOUT");
                        bool foundRemainingMillis = content.Contains("remainingMillis");

                        if (foundLeaverBusted) sb.AppendLine("✓ Found 'LEAVER_BUSTED' events");
                        if (foundLeaverBusterLockout) sb.AppendLine("✓ Found 'LEAVER_BUSTER_QUEUE_LOCKOUT' events");
                        if (foundRemainingMillis) sb.AppendLine("✓ Found 'remainingMillis' field");
                        
                        if (!foundLeaverBusted && !foundLeaverBusterLockout)
                        {
                            sb.AppendLine("✗ No punishment events found in logs");
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Error reading files: {ex.Message}");
                }
            }
            sb.AppendLine();

            // Check Accounts
            sb.AppendLine("--- Accounts ---");
            sb.AppendLine($"Total accounts: {config.Accounts.Count}");
            foreach (var account in config.Accounts)
            {
                sb.AppendLine($"\nAccount: {account.Username}");
                sb.AppendLine($"  AccountId: {(string.IsNullOrEmpty(account.AccountId) ? "NOT SET" : account.AccountId)}");
                sb.AppendLine($"  GameName: {(string.IsNullOrEmpty(account.GameName) ? "NOT SET" : account.GameName)}");
                sb.AppendLine($"  TagLine: {(string.IsNullOrEmpty(account.TagLine) ? "NOT SET" : account.TagLine)}");
                sb.AppendLine($"  LowPrioUntil: {(account.LowPrioUntil.HasValue ? account.LowPrioUntil.Value.ToString() : "NONE")}");
                sb.AppendLine($"  LockoutUntil: {(account.LockoutUntil.HasValue ? account.LockoutUntil.Value.ToString() : "NONE")}");
            }

            sb.AppendLine();
            sb.AppendLine("=== END DIAGNOSTICS ===");

            return sb.ToString();
        }
    }
}
