using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Services
{
    public class RiotLogDetectionService
    {
        // Tracks log state before login
        private static string _lastLeagueLogFile = string.Empty;
        private static long _lastLeagueLogLength = 0;
        private static DateTime _loginStartTime = DateTime.MinValue;

        /// <summary>
        /// Call this BEFORE starting League Client to record current log state
        /// </summary>
        public static void RecordLogStateBeforeLogin(string leagueClientLogsPath)
        {
            try
            {
                _loginStartTime = DateTime.Now;
                
                if (!Directory.Exists(leagueClientLogsPath))
                {
                    _lastLeagueLogFile = string.Empty;
                    _lastLeagueLogLength = 0;
                    return;
                }

                var logFiles = Directory.GetFiles(leagueClientLogsPath, "*.*")
                    .Where(f => f.EndsWith(".log") || f.EndsWith(".json"))
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                if (logFiles.Any())
                {
                    _lastLeagueLogFile = logFiles.First();
                    _lastLeagueLogLength = new FileInfo(_lastLeagueLogFile).Length;
                }
                else
                {
                    _lastLeagueLogFile = string.Empty;
                    _lastLeagueLogLength = 0;
                }
            }
            catch
            {
                _lastLeagueLogFile = string.Empty;
                _lastLeagueLogLength = 0;
                _loginStartTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Detects account info (accountId, gameName, tagLine) from League Client JSON logs
        /// Only looks at the MOST RECENT log file to avoid picking up old account data
        /// </summary>
        public static bool DetectAccountInfo(Account account, string leagueClientLogsPath, out string debugInfo)
        {
            debugInfo = "";
            try
            {
                if (!Directory.Exists(leagueClientLogsPath))
                {
                    debugInfo = $"Directory not found: {leagueClientLogsPath}";
                    return false;
                }

                // Get log files, but prioritize NEW files created after login started
                var allFiles = Directory.GetFiles(leagueClientLogsPath, "*.*")
                    .Where(f => f.EndsWith(".log") || f.EndsWith(".json"))
                    .Select(f => new FileInfo(f))
                    .ToList();

                if (!allFiles.Any())
                {
                    debugInfo = $"No log files found in: {leagueClientLogsPath}";
                    return false;
                }

                // If we tracked a previous file, ONLY use files created AFTER the login started
                // This avoids reading from a file that an already-open client is actively writing to
                var filesToSearch = allFiles;
                if (!string.IsNullOrEmpty(_lastLeagueLogFile) && _loginStartTime != DateTime.MinValue)
                {
                    // Filter to files created AFTER login started (ignore the old active file)
                    var newFiles = allFiles.Where(f => f.LastWriteTime > _loginStartTime).ToList();
                    
                    if (newFiles.Any())
                    {
                        debugInfo = $"Found {allFiles.Count} total files, {newFiles.Count} created after login started. Using NEW files only.\n";
                        filesToSearch = newFiles;
                    }
                    else
                    {
                        debugInfo = $"Found {allFiles.Count} total files, but NONE created after login. Checking all files...\n";
                    }
                }
                else
                {
                    debugInfo = $"Found {allFiles.Count} log files. Checking up to 3 most recent...\n";
                }

                var jsonFiles = filesToSearch.OrderByDescending(f => f.LastWriteTime).Take(3); // Try up to 3 files

                foreach (var fileInfo in jsonFiles)
                {
                    var logFile = fileInfo.FullName;
                    debugInfo += $"\nChecking: {Path.GetFileName(logFile)}\n";
                    try
                    {
                        debugInfo += $"  Size: {fileInfo.Length} bytes, Modified: {fileInfo.LastWriteTime}\n";
                        
                        // Try to read file with shared access to avoid lock issues
                        using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fileStream))
                        {
                            var allLines = new System.Collections.Generic.List<string>();
                            
                            // If this is the same file we tracked before login, skip to the new data
                            if (logFile == _lastLeagueLogFile && _lastLeagueLogLength > 0)
                            {
                                debugInfo += $"  Same file as before login, seeking to position {_lastLeagueLogLength}\n";
                                
                                // Read all lines but we'll only search the NEW ones
                                while (!reader.EndOfStream)
                                {
                                    allLines.Add(reader.ReadLine());
                                }
                                
                                // Calculate which lines are NEW (approximately)
                                // Assume average line is ~200 bytes
                                long bytesNew = fileStream.Length - _lastLeagueLogLength;
                                int approximateNewLines = Math.Max((int)(bytesNew / 200), 100);
                                int startSearchIndex = Math.Max(0, allLines.Count - approximateNewLines);
                                
                                debugInfo += $"  Total lines: {allLines.Count}, searching only from line {startSearchIndex + 1} (NEW data)\n";
                                
                                // Only search the NEW lines
                                allLines = allLines.Skip(startSearchIndex).ToList();
                            }
                            else
                            {
                                // Different file or first time - read all
                                while (!reader.EndOfStream)
                                {
                                    allLines.Add(reader.ReadLine());
                                }
                                debugInfo += $"  Total lines: {allLines.Count} (all data)\n";
                            }
                            
                            // ROBUST METHOD: Search backward through lines and stop at FIRST account found
                            // This ensures we only get the most recently logged-in account
                            string foundAccountId = null;
                            string foundGameName = null;
                            string foundTagLine = null;
                            
                            // Search from end backward
                            for (int i = allLines.Count - 1; i >= 0; i--)
                            {
                                var line = allLines[i];
                                
                                // Look for accountId
                                if (string.IsNullOrEmpty(foundAccountId))
                                {
                                    var accountIdMatch = Regex.Match(line, @"accountId[=:](\d+)", RegexOptions.IgnoreCase);
                                    if (!accountIdMatch.Success)
                                    {
                                        accountIdMatch = Regex.Match(line, @"""accountId[""']?\s*:\s*[""']?(\d+)", RegexOptions.IgnoreCase);
                                    }
                                    if (accountIdMatch.Success)
                                    {
                                        foundAccountId = accountIdMatch.Groups[1].Value;
                                    }
                                }
                                
                                // Look for gameName
                                if (string.IsNullOrEmpty(foundGameName))
                                {
                                    var gameNameMatch = Regex.Match(line, @"gameName[=:]([^&\s""',}]+)", RegexOptions.IgnoreCase);
                                    if (!gameNameMatch.Success)
                                    {
                                        gameNameMatch = Regex.Match(line, @"""gameName""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                                    }
                                    if (gameNameMatch.Success)
                                    {
                                        foundGameName = gameNameMatch.Groups[1].Value.Trim();
                                    }
                                }
                                
                                // Look for tagLine
                                if (string.IsNullOrEmpty(foundTagLine))
                                {
                                    var tagLineMatch = Regex.Match(line, @"tagLine[=:]([^&\s""',}]+)", RegexOptions.IgnoreCase);
                                    if (!tagLineMatch.Success)
                                    {
                                        tagLineMatch = Regex.Match(line, @"""tagLine""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                                    }
                                    if (tagLineMatch.Success)
                                    {
                                        foundTagLine = tagLineMatch.Groups[1].Value.Trim();
                                    }
                                }
                                
                                // STOP as soon as we find BOTH accountId AND gameName
                                // This ensures we get the MOST RECENT account only
                                if (!string.IsNullOrEmpty(foundAccountId) && !string.IsNullOrEmpty(foundGameName))
                                {
                                    debugInfo += $"  Found most recent account at line {i + 1}/{allLines.Count}\n";
                                    debugInfo += $"  AccountId: {foundAccountId}, GameName: {foundGameName}\n";
                                    
                                    // If account already has AccountId, verify it matches
                                    if (!string.IsNullOrEmpty(account.AccountId))
                                    {
                                        if (account.AccountId == foundAccountId)
                                        {
                                            account.GameName = foundGameName;
                                            if (!string.IsNullOrEmpty(foundTagLine))
                                                account.TagLine = foundTagLine;
                                            return true;
                                        }
                                        else
                                        {
                                            debugInfo += $"  AccountId mismatch: expected {account.AccountId}, found {foundAccountId}\n";
                                        }
                                    }
                                    else
                                    {
                                        // First time detection - bind all values
                                        account.AccountId = foundAccountId;
                                        account.GameName = foundGameName;
                                        if (!string.IsNullOrEmpty(foundTagLine))
                                            account.TagLine = foundTagLine;
                                        return true;
                                    }
                                    
                                    // If we found data but it doesn't match, stop searching
                                    break;
                                }
                            }
                            
                            debugInfo += $"  No complete account data found in this file\n";
                        }
                    }
                    catch (Exception ex)
                    {
                        debugInfo += $"  Error reading file (skipping): {ex.Message}\n";
                        continue; // Try next file
                    }
                }
                
                debugInfo += "\nDetection failed - no matching data found";
            }
            catch (Exception ex)
            {
                debugInfo = $"Exception: {ex.Message}";
            }

            return false;
        }

        // Overload for backward compatibility
        public static bool DetectAccountInfo(Account account, string leagueClientLogsPath)
        {
            return DetectAccountInfo(account, leagueClientLogsPath, out _);
        }

        /// <summary>
        /// Detects punishment events from Riot Client logs
        /// </summary>
        public static void DetectPunishments(Account account, string riotClientLogsPath)
        {
            try
            {
                if (!Directory.Exists(riotClientLogsPath))
                    return;

                // Skip if account doesn't have AccountId yet
                if (string.IsNullOrEmpty(account.AccountId))
                    return;

                var currentLogFiles = Directory.GetFiles(riotClientLogsPath, "*.log")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                if (!currentLogFiles.Any())
                    return;

                // Read the entire most recent file to catch all penalties
                var newestLogFile = currentLogFiles.First();
                string logSegment = File.ReadAllText(newestLogFile);

                if (string.IsNullOrEmpty(logSegment))
                    return;

                // Parse punishment events
                ParsePunishmentEvents(account, logSegment);
            }
            catch
            {
                // Detection failed
            }
        }

        private static void ParsePunishmentEvents(Account account, string logSegment)
        {
            try
            {
                // Search for this account's ID (handles both "accountId" and \"accountId\")
                var pattern = $@"\\?""accountId\\?""[:\s]*{account.AccountId}";
                var accountMatches = Regex.Matches(logSegment, pattern);
                
                if (accountMatches.Count == 0)
                    return; // Account not found in logs
                
                // For each occurrence, look for penalty data in larger context
                foreach (Match accountMatch in accountMatches)
                {
                    // Get 2000 chars of context (before and after)
                    int start = Math.Max(0, accountMatch.Index - 1000);
                    int length = Math.Min(logSegment.Length - start, 2000);
                    string context = logSegment.Substring(start, length);
                    
                    // Look for LEAVER_BUSTED with remainingMillis (handles escaped quotes)
                    // Pattern: \"LEAVER_BUSTED\",\"remainingMillis\":900000 or "LEAVER_BUSTED","remainingMillis":900000
                    var leaverMatch = Regex.Match(context, @"LEAVER_BUSTED\\?""[,\s]*\\?""remainingMillis\\?""[:\s]*(\d+)");
                    if (leaverMatch.Success && long.TryParse(leaverMatch.Groups[1].Value, out long lpqMillis))
                    {
                        var newLowPrioUntil = DateTime.Now.AddMilliseconds(lpqMillis);
                        
                        // ONLY update if new penalty is LATER than existing one (or no existing penalty)
                        // This ensures penalties persist and don't disappear when logs rotate
                        if (!account.LowPrioUntil.HasValue || newLowPrioUntil > account.LowPrioUntil.Value)
                        {
                            account.LowPrioUntil = newLowPrioUntil;
                            account.LowPrioMinutes = (int)Math.Ceiling(lpqMillis / 60000.0);
                        }
                    }
                    
                    // Look for LEAVER_BUSTER_QUEUE_LOCKOUT with remainingMillis
                    var lockoutMatch = Regex.Match(context, @"LEAVER_BUSTER_QUEUE_LOCKOUT\\?""[,\s]*\\?""remainingMillis\\?""[:\s]*(\d+)");
                    if (lockoutMatch.Success && long.TryParse(lockoutMatch.Groups[1].Value, out long lockoutMillis))
                    {
                        var newLockoutUntil = DateTime.Now.AddMilliseconds(lockoutMillis);
                        
                        // ONLY update if new lockout is LATER than existing one (or no existing lockout)
                        if (!account.LockoutUntil.HasValue || newLockoutUntil > account.LockoutUntil.Value)
                        {
                            account.LockoutUntil = newLockoutUntil;
                        }
                    }
                }
            }
            catch
            {
                // Parse failed
            }
        }

        /// <summary>
        /// Optional: Global sync to initialize punishment timers on app startup
        /// </summary>
        public static void GlobalSyncPunishments(Account account, string riotClientLogsPath)
        {
            try
            {
                if (!Directory.Exists(riotClientLogsPath))
                    return;

                if (string.IsNullOrEmpty(account.AccountId))
                    return;

                // Read up to 5 most recent log files to find penalty data
                var logFiles = Directory.GetFiles(riotClientLogsPath, "*.log")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .Take(5);

                foreach (var logFile in logFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(logFile);
                        ParsePunishmentEvents(account, content);
                        
                        // If we found penalties, no need to check more files
                        if (account.LowPrioUntil.HasValue || account.LockoutUntil.HasValue)
                            break;
                    }
                    catch
                    {
                        // Continue to next file
                    }
                }
            }
            catch
            {
                // Sync failed
            }
        }
    }
}
