using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Feli.RocketMod.Teleporting.Commands
{
    internal class TpaCommand : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = Plugin.Instance;
            var messageColor = plugin.MessageColor;
            var messageIcon = plugin.Configuration.Instance.MessageIcon;

            if (command.Length < 1)
            {
                Say(caller, plugin.Translate("TpaCommand:WrongUsage"), messageColor, messageIcon);
                return;
            }

            var player = (UnturnedPlayer) caller;

            var type = command[0].ToLower();

            var teleportsManager = plugin.TeleportsManager;

            if (type == "s" | type == "send")
            {
                if (command.Length < 2)
                {
                    Say(caller, plugin.Translate("TpaCommand:WrongUsage:Send"), messageColor, messageIcon);
                    return;
                }

                var target = UnturnedPlayer.FromName(command[1]);

                if (target == null)
                {
                    Say(caller, plugin.Translate("TpaCommand:WrongUsage:NotFound", command[1]), messageColor,
                        messageIcon);
                    return;
                }

                teleportsManager.Send(player, target);
            }
            else if (type == "a" | type == "accept")
                teleportsManager.Accept(player);
            else if (type == "c" | type == "cancel")
                teleportsManager.Cancel(player);
            else if (type == "l" | type == "list")
                teleportsManager.List(player);
            else
            {
                var target = UnturnedPlayer.FromName(command[0]);

                if (target == null)
                {
                    Say(caller, plugin.Translate("TpaCommand:WrongUsage:NotFound", command[0]), messageColor,
                        messageIcon);
                    return;
                }

                teleportsManager.Send(player, target);
            }
        }

        private void Say(IRocketPlayer rocketPlayer, string message, Color messageColor, string icon = null)
        {
            var player = rocketPlayer as UnturnedPlayer;
            
            ChatManager.serverSendMessage(message, messageColor, toPlayer: player.SteamPlayer(), mode: EChatMode.SAY, iconURL: icon, useRichTextFormatting: true);
        }
        
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "tpa";
        public string Help => "Send, accept, deny and cancel teleport requests";
        public string Syntax => "<playerName|accept|send|cancel|list>";
        public List<string> Aliases => new List<string>()
        {
            "tpr"
        };
        public List<string> Permissions => new List<string>();
    }
}