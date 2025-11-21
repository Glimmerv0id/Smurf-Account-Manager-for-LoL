using System;
using System.Collections.Generic;
using System.IO;

namespace SmurfAccountManager.Models
{
    public class AppConfig
    {
        public string RiotGamesPath { get; set; } = @"C:\Riot Games";
        public List<Account> Accounts { get; set; } = new List<Account>();

        // 1.1 Spec: Configurable log paths
        public string RiotClientLogsPath
        {
            get
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "Riot Games", "Riot Client", "Logs", "Riot Client Logs");
            }
        }

        public string LeagueClientLogsPath
        {
            get
            {
                return Path.Combine(RiotGamesPath, "League of Legends", "Logs", "LeagueClient Logs");
            }
        }
    }
}
