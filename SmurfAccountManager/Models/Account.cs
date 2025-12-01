using System;

namespace SmurfAccountManager.Models
{
    public enum AccountTag
    {
        None,
        YellowStar,
        RedCircle,
        GreenCircle
    }

    public class Account
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        
        // 1.1 Spec: New fields
        public string AccountId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string TagLine { get; set; } = string.Empty;
        
        // Penalty tracking
        public int? LowPriorityMinutes { get; set; } // Fixed penalty time (only counts down when queuing)
        public DateTime? LockoutUntil { get; set; } // Expiration time (always counting down)
        
        // Tag for visual organization
        public AccountTag Tag { get; set; } = AccountTag.None;
        
        public int DisplayOrder { get; set; }

        // Computed properties for UI display
        public string QueueLockoutRemaining
        {
            get
            {
                if (!LockoutUntil.HasValue || LockoutUntil.Value <= DateTime.Now)
                    return string.Empty;

                var remaining = LockoutUntil.Value - DateTime.Now;
                if (remaining.TotalHours >= 1)
                    return $"{(int)remaining.TotalHours}H {remaining.Minutes}M";
                else if (remaining.TotalMinutes >= 1)
                    return $"{(int)remaining.TotalMinutes}M";
                else
                    return "< 1M";
            }
        }

        public string FullRiotId
        {
            get
            {
                if (string.IsNullOrEmpty(GameName))
                    return string.Empty;
                if (string.IsNullOrEmpty(TagLine))
                    return GameName;
                return $"{GameName}#{TagLine}";
            }
        }

        public bool HasQueueLockout
        {
            get
            {
                return LockoutUntil.HasValue && LockoutUntil.Value > DateTime.Now;
            }
        }
    }
}
