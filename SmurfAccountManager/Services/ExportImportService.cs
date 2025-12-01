using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Services
{
    public static class ExportImportService
    {
        /// <summary>
        /// Generates a secure 16-character password with letters and numbers
        /// </summary>
        public static string GenerateExportPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const int length = 16;
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                
                var result = new StringBuilder(length);
                foreach (byte b in bytes)
                {
                    result.Append(chars[b % chars.Length]);
                }
                
                return result.ToString();
            }
        }

        /// <summary>
        /// Exports accounts to a text file with password encryption
        /// </summary>
        public static void ExportAccounts(List<Account> accounts, string filePath, string password)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Smurf Account Manager Export");
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Accounts: {accounts.Count}");
            sb.AppendLine();

            foreach (var account in accounts)
            {
                sb.AppendLine("[Account]");
                sb.AppendLine($"Username={account.Username}");
                sb.AppendLine($"AccountId={account.AccountId}");
                sb.AppendLine($"GameName={account.GameName}");
                sb.AppendLine($"TagLine={account.TagLine}");
                
                // Decrypt password from DPAPI encryption
                var plainPassword = EncryptionService.Decrypt(account.EncryptedPassword);
                // Re-encrypt with export password using AES
                var exportEncryptedPassword = EncryptWithPassword(plainPassword, password);
                sb.AppendLine($"EncryptedPassword={exportEncryptedPassword}");
                
                sb.AppendLine($"DisplayOrder={account.DisplayOrder}");
                sb.AppendLine($"Tag={account.Tag}");
                
                if (account.LowPriorityMinutes.HasValue)
                    sb.AppendLine($"LowPriorityMinutes={account.LowPriorityMinutes.Value}");
                
                if (account.LockoutUntil.HasValue)
                    sb.AppendLine($"LockoutUntil={account.LockoutUntil.Value:O}");
                
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            LoggerService.Info($"[Export] Exported {accounts.Count} accounts to {filePath}");
        }

        /// <summary>
        /// Imports accounts from a text file with password decryption
        /// </summary>
        public static List<Account> ImportAccounts(string filePath, string password)
        {
            var accounts = new List<Account>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            
            Account currentAccount = null;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;
                
                if (trimmed == "[Account]")
                {
                    if (currentAccount != null)
                        accounts.Add(currentAccount);
                    
                    currentAccount = new Account();
                    continue;
                }
                
                if (currentAccount != null && trimmed.Contains("="))
                {
                    var parts = trimmed.Split(new[] { '=' }, 2);
                    var key = parts[0].Trim();
                    var value = parts.Length > 1 ? parts[1].Trim() : "";
                    
                    switch (key)
                    {
                        case "Username":
                            currentAccount.Username = value;
                            break;
                        case "AccountId":
                            currentAccount.AccountId = value;
                            break;
                        case "GameName":
                            currentAccount.GameName = value;
                            break;
                        case "TagLine":
                            currentAccount.TagLine = value;
                            break;
                        case "EncryptedPassword":
                            try
                            {
                                // Decrypt from export password
                                var plainPassword = DecryptWithPassword(value, password);
                                // Re-encrypt with DPAPI for storage
                                currentAccount.EncryptedPassword = EncryptionService.Encrypt(plainPassword);
                            }
                            catch
                            {
                                throw new Exception("Invalid password or corrupted file");
                            }
                            break;
                        case "DisplayOrder":
                            if (int.TryParse(value, out int order))
                                currentAccount.DisplayOrder = order;
                            break;
                        case "Tag":
                            if (Enum.TryParse<AccountTag>(value, out AccountTag tag))
                                currentAccount.Tag = tag;
                            break;
                        case "LowPriorityMinutes":
                            if (int.TryParse(value, out int lpq))
                                currentAccount.LowPriorityMinutes = lpq;
                            break;
                        case "LockoutUntil":
                            if (DateTime.TryParse(value, out DateTime lockout))
                                currentAccount.LockoutUntil = lockout;
                            break;
                    }
                }
            }
            
            // Add the last account
            if (currentAccount != null)
                accounts.Add(currentAccount);
            
            LoggerService.Info($"[Import] Imported {accounts.Count} accounts from {filePath}");
            return accounts;
        }

        /// <summary>
        /// Encrypts text using AES with a password-derived key
        /// </summary>
        private static string EncryptWithPassword(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            
            using (var aes = Aes.Create())
            {
                // Derive key from password
                var salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
                {
                    aes.Key = deriveBytes.GetBytes(32); // 256 bits
                    aes.IV = deriveBytes.GetBytes(16);  // 128 bits
                }
                
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }
                    
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts text using AES with a password-derived key
        /// </summary>
        private static string DecryptWithPassword(string encryptedText, string password)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;
            
            using (var aes = Aes.Create())
            {
                // Derive key from password (same salt as encryption)
                var salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
                {
                    aes.Key = deriveBytes.GetBytes(32);
                    aes.IV = deriveBytes.GetBytes(16);
                }
                
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
