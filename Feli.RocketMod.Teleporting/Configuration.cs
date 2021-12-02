using Feli.RocketMod.Teleporting.Economy.Configuration;
using Rocket.API;
using SDG.Unturned;

namespace Feli.RocketMod.Teleporting
{
    public class Configuration : IRocketPluginConfiguration
    {
        public string MessageColor { get; set; }
        public string MessageIcon { get; set; }
        public float TeleportDelay { get; set; }
        public double TeleportCooldown { get; set; }
        public bool CancelWhenMove { get; set; }
        public bool TeleportProtection { get; set; }
        public double TeleportProtectionTime { get; set; }
        public bool TeleportCombatAllowed { get; set; }
        public double TeleportCombatTime { get; set; }
        public bool AllowAcceptingWithKeys { get; set; }
        public TeleportCost TeleportCost { get; set; }
        
        public void LoadDefaults()
        {
            MessageColor = "magenta";
            MessageIcon = Provider.configData.Browser.Icon;
            TeleportDelay = 5;
            TeleportCooldown = 60;
            CancelWhenMove = false;
            TeleportProtection = true;
            TeleportProtectionTime = 5;
            TeleportCombatAllowed = false;
            TeleportCombatTime = 30;
            AllowAcceptingWithKeys = true;
            TeleportCost = new TeleportCost()
            {
                Enabled = false,
                UseXp = true,
                TpaCost = 10
            };
        }
    }
}