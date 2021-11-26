using Feli.RocketMod.Teleporting.Economy;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Feli.RocketMod.Teleporting
{
    public class Plugin : RocketPlugin<Configuration>
    {
        public static Plugin Instance { get; private set; }
        public TeleportsManager TeleportsManager { get; private set; }
        public IEconomyProvider EconomyProvider { get; set; }
        public Color MessageColor { get; private set; }
        
        protected override void Load()
        {
            Instance = this;
            MessageColor = UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor, Color.green);
            TeleportsManager = new TeleportsManager(this);
            EconomyProvider = Configuration.Instance.TeleportCost.UseXp
                ? new ExperienceEconomyProvider() as IEconomyProvider
                : new UconomyEconomyProvider();

            Logger.Log($"Teleporting plugin v{Assembly.GetName().Version} loaded !");
            Logger.Log("Do you want more cool plugins? Join now: https://discord.gg/4FF2548 !");
            Logger.Log($"Economy Provider: {EconomyProvider.GetType().Name}");
        }

        protected override void Unload()
        {
            Instance = null;
            TeleportsManager.Dispose();
            TeleportsManager = null;
            EconomyProvider = null;
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            {"TpaCommand:WrongUsage", "Correct command usage: /tpa <accept|send|cancel>"},
            {"TpaCommand:WrongUsage:Send", "Correct command usage: /tpa send <playerName>"},
            {"TpaCommand:WrongUsage:NotFound", "Player with name {0} was not found"},
            {"TpaCommand:Send:Yourself", "There is no point on sending a tpa request to yourself"},
            {"TpaCommand:Send:Target", "{0} has just sent you a tpa request. Use \"/tpa accept\" to accept it or \n/tpa cancel\n to cancel it"},
            {"TpaCommand:Send:Sender", "Successfully sent a tpa request to {0}. Use \"/tpa cancel\" to cancel it"},
            {"TpaCommand:Accept:NoRequests", "There are no tpa requests to accept"},
            {"TpaCommand:Accept:Delay", "You will be teleported to {0} in {1} seconds"},
            {"TpaCommand:Accept:Success", "Successfully accepted {0}'s tpa"},
            {"TpaCommand:Accept:Teleported", "Successfully teleported to {0}"},
            {"TpaCommand:Cancel:NotRequests", "There are no tpa requests to cancel"},
            {"TpaCommand:Cancel:Other", "{0} has just cancelled the tpa request"},
            {"TpaCommand:Cancel:Success", "Successfully canceled the tpa with {0}"},
            {"TpaValidation:Car:Other", "The teleport was cancelled because {0} is on a car"},
            {"TpaValidation:Car:Self", "The teleport was cancelled because you are on a car"},
            {"TpaValidation:Leave", "The teleport was cancelled because {0} left the server"},
            {"TpaValidation:Balance:Sender", "You dont have enough balance to teleport. Teleport cost: {0}"},
            {"TpaValidation:Balance:Target", "The teleport was cancelled because {0} does not have enough balance"}
        };
    }
}