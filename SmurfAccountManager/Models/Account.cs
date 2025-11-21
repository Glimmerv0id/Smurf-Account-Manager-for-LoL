using System;

namespace SmurfAccountManager.Models
{
    public class Account
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        
        // 1.1 Spec: New fields
        public string AccountId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string TagLine { get; set; } = string.Empty;
        public DateTime? LowPrioUntil { get; set; }
        public int LowPrioMinutes { get; set; } // Fixed number of minutes for display
        public DateTime? LockoutUntil { get; set; }
        
        public int DisplayOrder { get; set; }

        // Computed properties for UI display
        public string LowPriorityQueueRemaining
        {
            get
            {
                // Show fixed minutes if penalty is still active
                if (LowPrioMinutes > 0 && LowPrioUntil.HasValue && LowPrioUntil.Value > DateTime.Now)
                    return $"{LowPrioMinutes} minutes";
                else
                    return string.Empty;
            }
        }

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
