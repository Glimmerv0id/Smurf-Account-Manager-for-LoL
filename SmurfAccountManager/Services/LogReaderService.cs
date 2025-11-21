using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmurfAccountManager.Services
{
    public class LogReaderService
    {
        public static string ReadSummonerName(string riotGamesPath)
        {
            try
            {
                // Try multiple possible log locations
                string[] possiblePaths = new[]
                {
                    Path.Combine(riotGamesPath, "League of Legends", "Logs", "LeagueClient Logs"),
                    Path.Combine(riotGamesPath, "League of Legends", "Logs", "GameLogs"),
                    Path.Combine(riotGamesPath, "League of Legends", "Logs")
                };

                foreach (var leagueLogsPath in possiblePaths)
                {
                    if (!Directory.Exists(leagueLogsPath))
                        continue;

                    // Find the most recent log files
                    var logFiles = Directory.GetFiles(leagueLogsPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".log") || f.EndsWith(".txt"))
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(10);

                    foreach (var logFile in logFiles)
                    {
                        try
                        {
                            // Read file in chunks to handle large files
                            var lines = File.ReadLines(logFile).Reverse().Take(5000);
                            
                            foreach (var line in lines)
                            {
                                // Try multiple patterns for summoner name
                                var patterns = new[]
                                {
                                    @"""displayName""\s*:\s*""([^""]+)""",
                                    @"""summonerName""\s*:\s*""([^""]+)""",
                                    @"""gameName""\s*:\s*""([^""]+)""",
                                    @"summoner.*?name.*?[""']([^""']+)[""']",
                                    @"display.*?name.*?[""']([^""']+)[""']",
                                    @"""name""\s*:\s*""([^""]+)"".*summoner"
                                };

                                foreach (var pattern in patterns)
                                {
                                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                                    if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                                    {
                                        var name = match.Groups[1].Value.Trim();
                                        // Filter out obvious non-summoner names
                                        if (name.Length >= 3 && name.Length <= 16 && 
                                            !name.Contains("http") && !name.Contains("riot") &&
                                            !name.Contains("league") && !name.Contains("client"))
                                        {
                                            return name;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Continue to next file if reading fails
                        }
                    }
                }
            }
            catch
            {
                // Return empty if path doesn't exist or can't be read
            }

            return string.Empty;
        }

        public static (DateTime? queueLockout, int lowPriorityMinutes) ReadQueuePenalties(string riotGamesPath)
        {
            try
            {
                // Try multiple possible log locations
                string[] possibleLogPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "Riot Client", "Logs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "League of Legends", "Logs"),
                    Path.Combine(riotGamesPath, "League of Legends", "Logs"),
                    Path.Combine(riotGamesPath, "Riot Client", "Logs")
                };

                DateTime? queueLockout = null;
                int lowPriorityMinutes = 0;

                foreach (var riotLogsPath in possibleLogPaths)
                {
                    if (!Directory.Exists(riotLogsPath))
                        continue;

                    // Find the most recent log files
                    var logFiles = Directory.GetFiles(riotLogsPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".log") || f.EndsWith(".txt"))
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(10);

                    foreach (var logFile in logFiles)
                    {
                        try
                        {
                            // Read most recent lines
                            var lines = File.ReadLines(logFile).Reverse().Take(2000);

                            foreach (var line in lines)
                            {
                                // Multiple patterns for queue lockout (hours)
                                var lockoutPatterns = new[]
                                {
                                    @"(?:queue|matchmaking|ranked).*?(?:lockout|lock|ban|restriction|suspended).*?(\d+)\s*(?:hour|hr|h)",
                                    @"(?:lockout|lock|ban|restriction|suspended).*?(?:queue|matchmaking|ranked).*?(\d+)\s*(?:hour|hr|h)",
                                    @"""penaltyTime[^:]*:\s*(\d+)",
                                    @"""timeUntilEligible[^:]*:\s*(\d+)",
                                    @"ranked.*?timer.*?(\d+)\s*(?:hour|hr|h)",
                                    @"(?:can|cannot).*?queue.*?(\d+)\s*(?:hour|hr|h)"
                                };

                                foreach (var pattern in lockoutPatterns)
                                {
                                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                                    if (match.Success && int.TryParse(match.Groups[1].Value, out int hours))
                                    {
                                        if (hours > 0 && hours < 720) // Reasonable range (0-30 days)
                                        {
                                            queueLockout = DateTime.Now.AddHours(hours);
                                            break;
                                        }
                                    }
                                }

                                // Multiple patterns for low priority queue (minutes)
                                var lpqPatterns = new[]
                                {
                                    @"(?:low|lower).*?priority.*?(?:queue|matchmaking).*?(\d+)\s*(?:minute|min|m)",
                                    @"(?:queue|matchmaking).*?(?:low|lower).*?priority.*?(\d+)\s*(?:minute|min|m)",
                                    @"""lowPriorityQueue[^:]*:\s*(\d+)",
                                    @"""waitTime[^:]*:\s*(\d+)",
                                    @"(?:lpq|LPQ).*?(\d+)\s*(?:minute|min|m)",
                                    @"priority.*?wait.*?(\d+)\s*(?:minute|min|m)"
                                };

                                foreach (var pattern in lpqPatterns)
                                {
                                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                                    if (match.Success && int.TryParse(match.Groups[1].Value, out int minutes))
                                    {
                                        if (minutes > 0 && minutes < 1000) // Reasonable range
                                        {
                                            lowPriorityMinutes = minutes;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Continue to next file
                        }
                    }

                    // If we found data, no need to check other paths
                    if (queueLockout.HasValue || lowPriorityMinutes > 0)
                        break;
                }

                return (queueLockout, lowPriorityMinutes);
            }
            catch
            {
                return (null, 0);
            }
        }
    }
}
