using System.Net.Configuration;
using Rocket.API;

namespace Feli.RocketMod.Teleporting
{
    public class Configuration : IRocketPluginConfiguration
    {
        public string MessageColor { get; set; }
        public float TeleportDelay { get; set; }
        public bool CancelWhenMove { get; set; }
        public void LoadDefaults()
        {
            MessageColor = "magenta";
            TeleportDelay = 5;
            CancelWhenMove = false;
        }
    }
}