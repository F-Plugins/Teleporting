using System;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;

[assembly: PluginMetadata("Feli.Teleporting",
    Author = "Feli",
    Description = "Makes teleportation possible on your server",
    DisplayName = "Teleporting",
    Website = "https://discord.gg/4FF2548"
)]

namespace Feli.OpenMod.Teleporting
{
    public class Plugin : OpenModUnturnedPlugin
    {
        private readonly ILogger<Plugin> _logger;
        
        public Plugin(
            ILogger<Plugin> logger,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
        }

        protected override UniTask OnLoadAsync()
        {
            _logger.LogInformation($"Teleporting plugin v{Version} loaded !");
            _logger.LogInformation("Do you want more cool plugins? Join now: https://discord.gg/4FF2548 !");
            
            return UniTask.CompletedTask;
        }
    }
}