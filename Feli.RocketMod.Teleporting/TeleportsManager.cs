using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Feli.RocketMod.Teleporting
{
    public class TeleportsManager : IDisposable
    {
        private readonly Plugin _plugin;
        private readonly Color _messageColor;
        private readonly Configuration _configuration;
        private List<Tuple<UnturnedPlayer, UnturnedPlayer>> _teleportRequests;

        public TeleportsManager(Plugin plugin)
        {
            _teleportRequests = new List<Tuple<UnturnedPlayer, UnturnedPlayer>>();
            _plugin = plugin;
            _configuration = plugin.Configuration.Instance;
            _messageColor = plugin.MessageColor;
            U.Events.OnPlayerDisconnected += OnLeave;
        }

        public void Send(UnturnedPlayer sender, UnturnedPlayer target)
        {
            if (sender.Equals(target))
            {
                UnturnedChat.Say(sender, _plugin.Translate("TpaCommand:Send:Yourself"), _messageColor, true);
                return;
            }

            var request = _teleportRequests.FirstOrDefault(x => x.Item1.Equals(sender) && x.Item2.Equals(target));
            
            if (request != null)
            {
                _teleportRequests.Remove(request);
            }

            request = new Tuple<UnturnedPlayer, UnturnedPlayer>(sender, target);
            
            _teleportRequests.Add(request);
            
            UnturnedChat.Say(sender, _plugin.Translate("TpaCommand:Send:Sender", target.DisplayName), _messageColor, true);
            UnturnedChat.Say(target, _plugin.Translate("TpaCommand:Send:Target", sender.DisplayName), _messageColor, true);
        }
        
        public void Accept(UnturnedPlayer target)
        {
            var request = _teleportRequests.FirstOrDefault(x => x.Item2.Equals(target));

            if (request == null)
            {
                UnturnedChat.Say(target, _plugin.Translate("TpaCommand:Accept:NoRequests"), _messageColor, true);
                return;
            }

            var sender = request.Item1;
            
            UnturnedChat.Say(target, _plugin.Translate("TpaCommand:Accept:Success", sender.DisplayName), _messageColor, true);
            if (_configuration.TeleportDelay > 0)
            {
                UnturnedChat.Say(sender, _plugin.Translate("TpaCommand:Accept:Delay", target.DisplayName, _configuration.TeleportDelay), _messageColor, true);
            }

            var senderPosition = sender.Position;
            
            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (!_teleportRequests.Any(x => x.Item2.Equals(target) && x.Item1.Equals(sender)))
                    return;
                
                if (_configuration.CancelWhenMove && senderPosition != sender.Position)
                {
                    return;
                }

                var isValid = ValidateRequest(request);

                if(!isValid)
                    return;

                if(_configuration.TeleportCost.Enabled)
                    _plugin.EconomyProvider.IncrementBalance(sender.Id, -_configuration.TeleportCost.TpaCost);
                
                sender.Player.teleportToLocationUnsafe(sender.Position, sender.Player.look.yaw);
                _teleportRequests.Remove(request);
                
                UnturnedChat.Say(sender, _plugin.Translate("TpaCommand:Accept:Teleported", target.DisplayName), _messageColor, true);
            }, _configuration.TeleportDelay);
        }

        public void Cancel(UnturnedPlayer player)
        {
            var request = _teleportRequests.FirstOrDefault(x => x.Item1.Equals(player) || x.Item2.Equals(player));

            if (request == null)
            {
                UnturnedChat.Say(player, _plugin.Translate("TpaCommand:Cancel:NotRequests"), _messageColor, true);
                return;
            }

            var other = request.Item2.Equals(player) ? request.Item1 : request.Item2;
            
            _teleportRequests.Remove(request);
            
            UnturnedChat.Say(player, _plugin.Translate("TpaCommand:Cancel:Success", other.DisplayName), _messageColor, true);
            UnturnedChat.Say(other, _plugin.Translate("TpaCommand:Cancel:Other", player.DisplayName), _messageColor, true);
        }

        private bool ValidateRequest(Tuple<UnturnedPlayer, UnturnedPlayer> request)
        {
            var sender = request.Item1;
            var target = request.Item2;

            if (sender.CurrentVehicle != null || target.CurrentVehicle != null)
            {
                var problem = sender.CurrentVehicle != null ? sender : target;

                UnturnedChat.Say(problem, _plugin.Translate("TpaValidation:Car:Self"), _messageColor, true);
                UnturnedChat.Say(sender, _plugin.Translate("TpaValidation:Car:Other", problem.DisplayName),
                    _messageColor, true);

                return false;
            }
            
            if (_configuration.TeleportCost.Enabled && _plugin.EconomyProvider.GetBalance(sender.Id) < _configuration.TeleportCost.TpaCost)
            {
                UnturnedChat.Say(sender, _plugin.Translate("TpaValidation:Balance:Sender", _configuration.TeleportCost.TpaCost), _messageColor, true);    
                UnturnedChat.Say(target, _plugin.Translate("TpaValidation:Balance:Target", sender.DisplayName), _messageColor, true);
                return false;
            }

            return true;
        }

        private void OnLeave(UnturnedPlayer player)
        {
            var requests = _teleportRequests.Where(x => x.Item1.Equals(player) | x.Item2.Equals(player));

            foreach (var request in requests.ToList())
            {
                var other = request.Item1.Equals(player) ? request.Item2 : request.Item1;
                
                UnturnedChat.Say(other, _plugin.Translate("TpaValidation:Leave", player.DisplayName), _messageColor, true);
                _teleportRequests.Remove(request);
            }
        }
        
        public void Dispose()
        {
            U.Events.OnPlayerDisconnected -= OnLeave;
        }
    }
}