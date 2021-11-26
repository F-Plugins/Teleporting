using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine.Playables;

namespace Feli.RocketMod.Teleporting.Commands
{
    internal class TpaCommand : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = Plugin.Instance;
            var messageColor = plugin.MessageColor;
            
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("TpaCommand:WrongUsage"), messageColor);
                return;
            }
            
            var player = (UnturnedPlayer) caller;
            
            var type = command[0].ToLower();
            
            var teleportsManager = plugin.TeleportsManager;

            if (type == "s" | type == "send")
            {
                if (command.Length < 2)
                {
                    UnturnedChat.Say(caller, plugin.Translate("TpaCommand:WrongUsage:Send"), messageColor);
                    return;
                }

                var target = UnturnedPlayer.FromName(command[1]);

                if (target == null)
                {
                    UnturnedChat.Say(caller, plugin.Translate("TpaCommand:WrongUsage:NotFound", command[1]), messageColor);
                    return;
                }
                
                teleportsManager.Send(player, target);
            }
            else if (type == "a" | type == "accept")
                teleportsManager.Accept(player);
            else if (type == "c" | type == "cancel")
                teleportsManager.Cancel(player);
            else
            {
                UnturnedChat.Say(caller, plugin.Translate("TpaCommand:WrongUsage"), messageColor);
            }
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "tpa";
        public string Help => "Send, accept, deny and cancel teleport requests";
        public string Syntax => "<accept|send|cancel>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();
    }
}