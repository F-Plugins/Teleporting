using Feli.RocketMod.Teleporting.Economy.Configuration;
using Rocket.API;

namespace Feli.RocketMod.Teleporting
{
    public class Configuration : IRocketPluginConfiguration
    {
        public string MessageColor { get; set; }
        public float TeleportDelay { get; set; }
        public double TeleportCooldown { get; set; }
        public bool CancelWhenMove { get; set; }
        public TeleportCost TeleportCost { get; set; }
        
        public void LoadDefaults()
        {
            MessageColor = "magenta";
            TeleportDelay = 5;
            TeleportCooldown = 60;
            CancelWhenMove = false;
            TeleportCost = new TeleportCost()
            {
                Enabled = false,
                UseXp = true,
                TpaCost = 10
            };
        }
    }
}