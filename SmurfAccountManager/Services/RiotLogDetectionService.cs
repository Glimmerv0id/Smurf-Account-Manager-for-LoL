using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Services
{
    public class RiotLogDetectionService
    {
        // Constants
        private const int MAX_JSON_FILES_TO_CHECK = 3;
        private const int MAX_LOG_FILES_TO_CHECK = 5;
        
        // Compiled regex patterns for better performance
        private static readonly Regex TimestampPattern = new Regex(
            @"(\d{4})-(\d{2})-(\d{2})T(\d{2})-(\d{2})-(\d{2})",
            RegexOptions.Compiled
        );
        
        private static readonly Regex AccountIdPattern = new Regex(
            @"\\?""accountId\\?""\s*:\s*(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        private static readonly Regex GameNamePattern = new Regex(
            @"\\?""gameName\\?""\s*:\s*\\?""([^\\""]+)\\?""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        private static readonly Regex TagLinePattern = new Regex(
            @"\\?""tagLine\\?""\s*:\s*\\?""([^\\""]+)\\?""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        private static readonly Regex LeaverBustedPattern = new Regex(
            @"LEAVER_BUSTED\\?""[,\s]*\\?""remainingMillis\\?""[:\s]*(\d+)",
            RegexOptions.Compiled
        );
        
        private static readonly Regex LeaverBusterLockoutPattern = new Regex(
            @"LEAVER_BUSTER_QUEUE_LOCKOUT\\?""[,\s]*\\?""remainingMillis\\?""[:\s]*(\d+)",
            RegexOptions.Compiled
        );

        // State tracking
        private static DateTime _loginStartTime = DateTime.MinValue;

        /// <summary>
        /// Call this BEFORE starting Riot/League Client to record current time
        /// Used to identify files created during this login session
        /// </summary>
        public static void RecordLogStateBeforeLogin(string leagueClientLogsPath)
        {
            _loginStartTime = DateTime.Now;
        }

        /// <summary>
        /// Detects account info (accountId, gameName, tagLine) from League Client JSON files
        /// Only looks at JSON files created after the login started
        /// </summary>
        public static bool DetectAccountInfo(Account account, string leagueClientLogsPath, out string debugInfo)
        {
            debugInfo = "";
            LoggerService.Info($"[DetectAccountInfo] Starting detection for account: {account.Username}");
            LoggerService.Debug($"[DetectAccountInfo] Search path: {leagueClientLogsPath}");
            LoggerService.Debug($"[DetectAccountInfo] Login start time: {_loginStartTime}");
            
            try
            {
                if (!Directory.Exists(leagueClientLogsPath))
                {
                    debugInfo = $"Directory not found: {leagueClientLogsPath}";
                    LoggerService.Error($"[DetectAccountInfo] Directory not found: {leagueClientLogsPath}");
                    return false;
                }

                // Find LeagueClient-tracing.json files created after login
                var jsonFiles = FindFilesAfter(
                    leagueClientLogsPath, 
                    _loginStartTime, 
                    "LeagueClient-tracing.json"
                );

                if (jsonFiles.Length == 0)
                {
                    debugInfo = "No new LeagueClient-tracing.json files found after login";
                    LoggerService.Warning($"[DetectAccountInfo] No JSON files found after {_loginStartTime}");
                    return false;
                }

                LoggerService.Info($"[DetectAccountInfo] Found {jsonFiles.Length} JSON files (checking up to {MAX_JSON_FILES_TO_CHECK})");
                debugInfo = $"Found {jsonFiles.Length} JSON files after login (checking up to {MAX_JSON_FILES_TO_CHECK})\n";

                // Try up to MAX_JSON_FILES_TO_CHECK most recent files
                foreach (var jsonFile in jsonFiles.Take(MAX_JSON_FILES_TO_CHECK))
                {
                    LoggerService.Debug($"[DetectAccountInfo] Checking file: {jsonFile.Name} ({jsonFile.Length} bytes, modified: {jsonFile.LastWriteTime})");
                    debugInfo += $"\nReading: {jsonFile.Name}\n";
                    debugInfo += $"  Created: {jsonFile.LastWriteTime}\n";
                    
                    try
                    {
                        var content = ReadFileWithRetry(jsonFile.FullName);
                        LoggerService.Debug($"[DetectAccountInfo] File read successfully: {content.Length} characters");
                        
                        if (ParseAccountFromJson(content, account, out string parseDetails))
                        {
                            debugInfo += parseDetails;
                            debugInfo += $"\n✓ Account detected successfully";
                            LoggerService.Info($"[DetectAccountInfo] ✓ Account detected: ID={account.AccountId}, Name={account.GameName}#{account.TagLine}");
                            return true;
                        }
                        else
                        {
                            debugInfo += parseDetails;
                            LoggerService.Debug($"[DetectAccountInfo] Parsing failed: {parseDetails}");
                        }
                    }
                    catch (Exception ex)
                    {
                        debugInfo += $"  Error reading file: {ex.Message}\n";
                        LoggerService.Error($"[DetectAccountInfo] Error reading {jsonFile.Name}", ex);
                        continue;
                    }
                }

                debugInfo += "\nNo account data found in checked JSON files";
                LoggerService.Warning($"[DetectAccountInfo] Failed to detect account after checking {jsonFiles.Take(MAX_JSON_FILES_TO_CHECK).Count()} files");
                return false;
            }
            catch (Exception ex)
            {
                debugInfo = $"Exception in DetectAccountInfo: {ex.Message}";
                LoggerService.Error("[DetectAccountInfo] Unexpected exception", ex);
                return false;
            }
        }

        // Overload for backward compatibility
        public static bool DetectAccountInfo(Account account, string leagueClientLogsPath)
        {
            return DetectAccountInfo(account, leagueClientLogsPath, out _);
        }

        /// <summary>
        /// Detects punishment events from Riot Client log files for all accounts
        /// Checks recent log files regardless of login time
        /// Searches for any account IDs in logs and matches them to provided accounts
        /// </summary>
        public static void DetectPunishments(System.Collections.Generic.IEnumerable<Account> accounts, string riotClientLogsPath)
        {
            try
            {
                if (!Directory.Exists(riotClientLogsPath))
                    return;

                // Create a dictionary for fast account lookup by AccountId
                var accountDict = accounts
                    .Where(a => !string.IsNullOrEmpty(a.AccountId))
                    .ToDictionary(a => a.AccountId, a => a);

                if (accountDict.Count == 0)
                {
                    LoggerService.Debug("[DetectPunishments] No accounts with AccountId to check");
                    return;
                }

                LoggerService.Info($"[DetectPunishments] Checking for punishments for {accountDict.Count} accounts");

                // Get recent Riot Client log files (regardless of login time)
                var logFiles = Directory.GetFiles(riotClientLogsPath, "*Riot Client.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Take(MAX_LOG_FILES_TO_CHECK);

                // Check up to MAX_LOG_FILES_TO_CHECK most recent files
                foreach (var logFile in logFiles)
                {
                    try
                    {
                        var content = ReadFileWithRetry(logFile.FullName);
                        
                        // Search for all account IDs in this log file
                        ParsePunishmentEventsForAllAccounts(accountDict, content);
                    }
                    catch (Exception ex)
                    {
                        LoggerService.Warning($"[DetectPunishments] Failed to read log file {logFile.Name}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error("[DetectPunishments] Unexpected exception", ex);
            }
        }

        /// <summary>
        /// Overload for backward compatibility - detects punishments for a single account
        /// </summary>
        public static void DetectPunishments(Account account, string riotClientLogsPath)
        {
            DetectPunishments(new[] { account }, riotClientLogsPath);
        }

        #region Helper Methods

        /// <summary>
        /// Reads a file with retry logic to handle file locks
        /// </summary>
        private static string ReadFileWithRetry(string filePath, int maxAttempts = 5, int delayMs = 500)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    // Use FileShare.ReadWrite to allow reading even if file is open by another process
                    using (var fileStream = new FileStream(filePath, 
                                                           FileMode.Open, 
                                                           FileAccess.Read, 
                                                           FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (IOException ex) when (attempt < maxAttempts)
                {
                    // File is locked or inaccessible, wait and retry
                    LoggerService.Warning($"[RiotLogDetectionService] File locked, attempt {attempt}/{maxAttempts}: {ex.Message}");
                    System.Threading.Thread.Sleep(delayMs);
                    continue;
                }
                catch (IOException ex)
                {
                    // Last attempt failed
                    LoggerService.Error($"[RiotLogDetectionService] Failed to read file after {maxAttempts} attempts", ex);
                    throw;
                }
            }
            
            throw new IOException($"Could not read file after {maxAttempts} attempts: {filePath}");
        }

        /// <summary>
        /// Finds files created after a specific time, optionally filtered by filename pattern
        /// </summary>
        private static FileInfo[] FindFilesAfter(string directory, DateTime after, string filenameContains = null)
        {
            if (!Directory.Exists(directory))
                return Array.Empty<FileInfo>();

            try
            {
                return Directory.GetFiles(directory)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime >= after)
                    .Where(f => filenameContains == null || f.Name.Contains(filenameContains))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Parses account information from JSON content
        /// </summary>
        private static bool ParseAccountFromJson(string jsonContent, Account account, out string details)
        {
            details = "";
            
            // Extract data using compiled regex patterns
            var accountIdMatch = AccountIdPattern.Match(jsonContent);
            var gameNameMatch = GameNamePattern.Match(jsonContent);
            var tagLineMatch = TagLinePattern.Match(jsonContent);

            if (!accountIdMatch.Success)
            {
                details = "  accountId not found\n";
                return false;
            }

            if (!gameNameMatch.Success)
            {
                details = "  gameName not found\n";
                return false;
            }

            var accountId = accountIdMatch.Groups[1].Value;
            var gameName = gameNameMatch.Groups[1].Value;
            var tagLine = tagLineMatch.Success ? tagLineMatch.Groups[1].Value : "";

            // Validate data
            if (!IsValidAccountId(accountId))
            {
                details = $"  Invalid accountId: {accountId}\n";
                return false;
            }

            if (!IsValidGameName(gameName))
            {
                details = $"  Invalid gameName: {gameName}\n";
                return false;
            }

            // If account already has accountId, verify it matches
            if (!string.IsNullOrEmpty(account.AccountId))
            {
                if (account.AccountId != accountId)
                {
                    details = $"  AccountId mismatch: expected {account.AccountId}, found {accountId}\n";
                    return false;
                }
            }

            // Set account data
            account.AccountId = accountId;
            account.GameName = gameName;
            if (!string.IsNullOrEmpty(tagLine))
                account.TagLine = tagLine;

            details = $"  ✓ accountId: {accountId}\n";
            details += $"  ✓ gameName: {gameName}\n";
            if (!string.IsNullOrEmpty(tagLine))
                details += $"  ✓ tagLine: {tagLine}\n";

            return true;
        }

        /// <summary>
        /// Parses punishment events from log content for a single account
        /// </summary>
        private static void ParsePunishmentEvents(Account account, string logContent)
        {
            try
            {
                // Search for accountId pattern in log
                var accountPattern = $@"\\?""accountId\\?""[:\s]*{account.AccountId}";
                var accountMatches = Regex.Matches(logContent, accountPattern);

                if (accountMatches.Count == 0)
                    return;

                // For each account mention, check surrounding context for penalties
                foreach (Match accountMatch in accountMatches)
                {
                    // Get context window around the match (500 chars before and after)
                    int start = Math.Max(0, accountMatch.Index - 500);
                    int length = Math.Min(logContent.Length - start, 1000);
                    string context = logContent.Substring(start, length);

                    // Look for LEAVER_BUSTED (Low Priority Queue)
                    var lpqMatch = LeaverBustedPattern.Match(context);
                    if (lpqMatch.Success && long.TryParse(lpqMatch.Groups[1].Value, out long lpqMillis))
                    {
                        int newLowPriorityMinutes = (int)Math.Ceiling(lpqMillis / 60000.0);

                        // FIXED: Always trust Riot's current value (not just when higher)
                        // This allows LPQ to persist correctly and be cleared when appropriate
                        if (newLowPriorityMinutes <= 0)
                        {
                            // Riot says penalty is complete - clear it
                            if (account.LowPriorityMinutes.HasValue)
                            {
                                LoggerService.Info($"[ParsePunishments] Clearing LPQ for account {account.AccountId} (Riot reports 0 minutes)");
                                account.LowPriorityMinutes = null;
                            }
                        }
                        else
                        {
                            // Update to Riot's current value, regardless if higher or lower
                            // LPQ is 15 minutes per game and stays constant, so this preserves it
                            if (!account.LowPriorityMinutes.HasValue)
                            {
                                LoggerService.Info($"[ParsePunishments] Setting LPQ for account {account.AccountId}: {newLowPriorityMinutes} minutes");
                            }
                            else if (account.LowPriorityMinutes.Value != newLowPriorityMinutes)
                            {
                                LoggerService.Info($"[ParsePunishments] Updating LPQ for account {account.AccountId}: {account.LowPriorityMinutes} -> {newLowPriorityMinutes} minutes");
                            }
                            account.LowPriorityMinutes = newLowPriorityMinutes;
                        }
                    }

                    // Look for LEAVER_BUSTER_QUEUE_LOCKOUT
                    var lockoutMatch = LeaverBusterLockoutPattern.Match(context);
                    if (lockoutMatch.Success && long.TryParse(lockoutMatch.Groups[1].Value, out long lockoutMillis))
                    {
                        var newLockoutUntil = DateTime.Now.AddMilliseconds(lockoutMillis);

                        // Only update if new lockout is later (or no existing lockout)
                        if (!account.LockoutUntil.HasValue || newLockoutUntil > account.LockoutUntil.Value)
                        {
                            account.LockoutUntil = newLockoutUntil;
                        }
                    }
                }
            }
            catch
            {
                // Parse failed silently
            }
        }

        /// <summary>
        /// Parses punishment events from log content for all accounts
        /// Searches the entire log for any account IDs and matches them to provided accounts
        /// </summary>
        private static void ParsePunishmentEventsForAllAccounts(System.Collections.Generic.Dictionary<string, Account> accountDict, string logContent)
        {
            try
            {
                // Find all accountId mentions in the log
                var accountIdMatches = AccountIdPattern.Matches(logContent);
                
                if (accountIdMatches.Count == 0)
                    return;

                // Track which accounts we've already processed in this log
                var processedAccounts = new System.Collections.Generic.HashSet<string>();

                foreach (Match match in accountIdMatches)
                {
                    var accountId = match.Groups[1].Value;
                    
                    // Check if this account ID belongs to one of our accounts
                    if (accountDict.TryGetValue(accountId, out Account account))
                    {
                        // Only process each account once per log file
                        if (processedAccounts.Add(accountId))
                        {
                            LoggerService.Debug($"[ParsePunishmentEventsForAllAccounts] Found account {accountId} in logs");
                            
                            // Parse punishment events for this account
                            ParsePunishmentEvents(account, logContent);
                        }
                    }
                }

                if (processedAccounts.Count > 0)
                {
                    LoggerService.Info($"[ParsePunishmentEventsForAllAccounts] Processed {processedAccounts.Count} accounts from logs");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error("[ParsePunishmentEventsForAllAccounts] Error parsing punishment events", ex);
            }
        }

        /// <summary>
        /// Validates account ID format
        /// </summary>
        private static bool IsValidAccountId(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                return false;

            // Account ID should be numeric and reasonable length (5-20 digits)
            return accountId.Length >= 5 
                && accountId.Length <= 20 
                && accountId.All(char.IsDigit);
        }

        /// <summary>
        /// Validates game name format
        /// </summary>
        private static bool IsValidGameName(string gameName)
        {
            if (string.IsNullOrEmpty(gameName))
                return false;

            // Game name should be 3-16 characters (Riot's rules)
            return gameName.Length >= 3 && gameName.Length <= 16;
        }

        #endregion
    }
}
