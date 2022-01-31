using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Feli.RocketMod.Teleporting
{
    public class TeleportsManager : IDisposable
    {
        private Plugin _plugin;
        private Color _messageColor;
        private string _messageIcon;
        private Configuration _configuration;
        private List<Tuple<UnturnedPlayer, UnturnedPlayer>> _teleportRequests;
        private Dictionary<UnturnedPlayer, DateTime> _cooldowns;
        private Dictionary<UnturnedPlayer, DateTime> _teleportProtections;
        private Dictionary<UnturnedPlayer, DateTime> _playersLastCombat;

        public TeleportsManager(Plugin plugin)
        {
            _teleportRequests = new List<Tuple<UnturnedPlayer, UnturnedPlayer>>();
            _teleportProtections = new Dictionary<UnturnedPlayer, DateTime>();
            _playersLastCombat = new Dictionary<UnturnedPlayer, DateTime>();
            _cooldowns = new Dictionary<UnturnedPlayer, DateTime>();
            _plugin = plugin;
            _configuration = plugin.Configuration.Instance;
            _messageIcon = _configuration.MessageIcon;
            _messageColor = plugin.MessageColor;
            U.Events.OnPlayerDisconnected += OnLeave;
            DamageTool.onPlayerAllowedToDamagePlayer += OnPlayerAllowedToDamagePlayer;
            PlayerAnimator.OnLeanChanged_Global += OnLeanChanged;
        }

        public void Send(UnturnedPlayer sender, UnturnedPlayer target)
        {
            if (sender.Equals(target))
            {
                Say(sender, _plugin.Translate("TpaCommand:Send:Yourself"), _messageColor, _messageIcon);
                return;
            }

            var request = _teleportRequests.FirstOrDefault(x => x.Item1.Equals(sender) && x.Item2.Equals(target));
            
            if (request != null)
            {
                _teleportRequests.Remove(request);
            }

            var cooldown = GetCooldown(sender);

            var cooldownTime = cooldown.AddSeconds(_configuration.TeleportCooldown);

            if (cooldownTime > DateTime.Now)
            {
                var waitTime = (cooldownTime - DateTime.Now).TotalSeconds;
                Say(sender, _plugin.Translate("TpaCommand:Send:Cooldown", Math.Round(waitTime)), _messageColor, _messageIcon);
                return;
            }
            
            if (!_configuration.TeleportCombatAllowed)
            {
                var combat = GetLastCombat(sender);

                var combatTime = combat.AddSeconds(_configuration.TeleportCombatTime);

                if (combatTime > DateTime.Now)
                {
                    var waitTime = (combatTime - DateTime.Now).TotalSeconds;
                    Say(sender, _plugin.Translate("TpaCommand:Send:Combat", Math.Round(waitTime)), _messageColor, _messageIcon);
                    return;
                }
            }
            
            UpdateCooldown(sender);
            
            request = new Tuple<UnturnedPlayer, UnturnedPlayer>(sender, target);
            
            _teleportRequests.Add(request);

            if (_configuration.AutoAcceptSameGroupRequests && sender.Player.quests.groupID != CSteamID.Nil &&
                target.Player.quests.groupID != CSteamID.Nil &&
                sender.Player.quests.groupID == target.Player.quests.groupID)
            {
                Accept(target, request);
                return;
            }
            
            Say(sender, _plugin.Translate("TpaCommand:Send:Sender", target.DisplayName), _messageColor, _messageIcon);
            Say(target, _plugin.Translate("TpaCommand:Send:Target", sender.DisplayName), _messageColor, _messageIcon);
        }
        
        public void Accept(UnturnedPlayer target, Tuple<UnturnedPlayer, UnturnedPlayer> request = null)
        {
            if (request == null)
            {
                request = _teleportRequests.FirstOrDefault(x => x.Item2.Equals(target));

                if (request == null)
                {
                    Say(target , _plugin.Translate("TpaCommand:Accept:NoRequests"), _messageColor, _messageIcon);
                    return;
                }
            }

            var sender = request.Item1;
            
            Say(target, _plugin.Translate("TpaCommand:Accept:Success", sender.DisplayName), _messageColor, _messageIcon);
            if (_configuration.TeleportDelay > 0)
            {
                Say(sender, _plugin.Translate("TpaCommand:Accept:Delay", target.DisplayName, _configuration.TeleportDelay), _messageColor, _messageIcon);
            }

            var senderPosition = sender.Position;
            
            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (!_teleportRequests.Any(x => x.Item2.Equals(target) && x.Item1.Equals(sender)))
                    return;
                
                if (_configuration.CancelWhenMove && senderPosition != sender.Position)
                {
                    Say(sender, _plugin.Translate("TpaValidation:Move:Sender"), _messageColor, _messageIcon);
                    Say(target, _plugin.Translate("TpaValidation:Move:Target", sender.DisplayName), _messageColor, _messageIcon);
                    return;
                }

                var isValid = ValidateRequest(request);

                if(!isValid)
                    return;

                if(_configuration.TeleportCost.Enabled)
                    _plugin.EconomyProvider.IncrementBalance(sender.Id, -_configuration.TeleportCost.TpaCost);
                
                UpdateTeleportProtection(sender);
                
                sender.Player.teleportToLocationUnsafe(target.Position, target.Player.look.yaw);
                _teleportRequests.Remove(request);
                
                Say(sender, _plugin.Translate("TpaCommand:Accept:Teleported", target.DisplayName), _messageColor, _messageIcon);
            }, _configuration.TeleportDelay);
        }

        public void List(UnturnedPlayer player)
        {
            var requests = _teleportRequests.Where(x => x.Item2.Equals(player));

            if (requests.Count() == 0)
            {
                Say(player, _plugin.Translate("TpaCommand:List:NotFound"), _messageColor, _messageIcon);
                return;
            }

            var playerNames = requests.Select(x => x.Item1.DisplayName);
            
            Say(player, _plugin.Translate("TpaCommand:List:Display", requests.Count()), _messageColor, _messageIcon);
            
            foreach (var playerName in playerNames)
            {
                Say(player, _plugin.Translate("TpaCommand:List:Section", playerName), _messageColor, _messageIcon);
            }
        }
        
        public void Cancel(UnturnedPlayer player)
        {
            var request = _teleportRequests.FirstOrDefault(x => x.Item1.Equals(player) || x.Item2.Equals(player));

            if (request == null)
            {
                Say(player, _plugin.Translate("TpaCommand:Cancel:NoRequests"), _messageColor, _messageIcon);
                return;
            }

            var other = request.Item2.Equals(player) ? request.Item1 : request.Item2;
            
            _teleportRequests.Remove(request);
            
            Say(player, _plugin.Translate("TpaCommand:Cancel:Success", other.DisplayName), _messageColor, _messageIcon);
            Say(other, _plugin.Translate("TpaCommand:Cancel:Other", player.DisplayName), _messageColor, _messageIcon);
        }

        private bool ValidateRequest(Tuple<UnturnedPlayer, UnturnedPlayer> request)
        {
            var sender = request.Item1;
            var target = request.Item2;

            if (sender.CurrentVehicle != null || target.CurrentVehicle != null)
            {
                var problem = sender.CurrentVehicle != null ? sender : target;
                var noProblem = sender.CurrentVehicle == null ? sender : target;
                
                Say(problem, _plugin.Translate("TpaValidation:Car:Self"), _messageColor, _messageIcon);
                Say(noProblem, _plugin.Translate("TpaValidation:Car:Other", problem.DisplayName),
                    _messageColor, _messageIcon);

                return false;
            }
            
            if(sender.Dead || target.Dead)
            {
                var dead = sender.Dead ? sender : target;
                var alive = !sender.Dead ? sender : target;

                Say(dead, _plugin.Translate("TpaValidation:Dead:Dead"), _messageColor, _messageIcon);
                Say(alive, _plugin.Translate("TpaValidation:Dead:Alive", dead.DisplayName), _messageColor, _messageIcon);

                return false;
            }

            if (_configuration.TeleportCost.Enabled && _plugin.EconomyProvider.GetBalance(sender.Id) < _configuration.TeleportCost.TpaCost)
            {
                Say(sender, _plugin.Translate("TpaValidation:Balance:Sender", _configuration.TeleportCost.TpaCost), _messageColor, _messageIcon);
                Say(target, _plugin.Translate("TpaValidation:Balance:Target", sender.DisplayName), _messageColor, _messageIcon);
                
                return false;
            }

            if (!_configuration.TeleportCombatAllowed)
            {
                var senderCombat = GetLastCombat(sender);
                var targetCombat = GetLastCombat(target);

                var senderCombatTime = senderCombat.AddSeconds(_configuration.TeleportCombatTime);
                var targetCombatTime = targetCombat.AddSeconds(_configuration.TeleportCombatTime);

                if(senderCombatTime > DateTime.Now)
                {
                    var waitTime = (senderCombatTime - DateTime.Now).TotalSeconds;

                    Say(sender, _plugin.Translate("TpaValidation:Combat:Self", Math.Round(waitTime)), _messageColor, _messageIcon);
                    Say(target, _plugin.Translate("TpaValidation:Combat:Other", sender.DisplayName), _messageColor, _messageIcon);

                    return false;
                }
                else if(targetCombatTime > DateTime.Now)
                {
                    var waitTime = (targetCombatTime - DateTime.Now).TotalSeconds;

                    Say(target, _plugin.Translate("TpaValidation:Combat:Self", Math.Round(waitTime)), _messageColor, _messageIcon);
                    Say(sender, _plugin.Translate("TpaValidation:Combat:Other", target.DisplayName), _messageColor, _messageIcon);

                    return false;
                }
            }

            return true;
        }

        private void OnLeave(UnturnedPlayer player)
        {
            var requests = _teleportRequests.Where(x => x.Item1.Equals(player) | x.Item2.Equals(player));

            foreach (var request in requests.ToList())
            {
                var other = request.Item1.Equals(player) ? request.Item2 : request.Item1;
                
                Say(other, _plugin.Translate("TpaValidation:Leave", player.DisplayName), _messageColor, _messageIcon);
                _teleportRequests.Remove(request);
            }

            var cooldown = GetCooldown(player);

            if (cooldown != DateTime.MinValue)
                _cooldowns.Remove(player);

            var protection = GetTeleportProtection(player);

            if (protection != DateTime.MinValue)
                _teleportProtections.Remove(player);

            var lastCombat = GetLastCombat(player);

            if (lastCombat != DateTime.MinValue)
                _playersLastCombat.Remove(player);
        }

        private void OnPlayerAllowedToDamagePlayer(Player nativeInstigator, Player nativeVictim, ref bool isAllowed)
        {
            var instigator = UnturnedPlayer.FromPlayer(nativeInstigator);

            UpdateLastCombat(instigator);
            
            if (!_configuration.TeleportProtection)
                return;
            
            var victim = UnturnedPlayer.FromPlayer(nativeVictim);
            
            if (_teleportProtections.ContainsKey(victim))
            {
                var teleportProtection = GetTeleportProtection(victim);

                var teleportProtectionTime = teleportProtection.AddSeconds(_configuration.TeleportProtectionTime);

                if (teleportProtection > DateTime.Now)
                {
                    isAllowed = false;
                    var waitTime = (teleportProtectionTime - DateTime.Now).TotalSeconds;

                    Say(instigator, _plugin.Translate("TpaProtection", Math.Round(waitTime), victim.DisplayName), _messageColor, _messageIcon);
                }
                else
                {
                    _teleportProtections.Remove(victim);
                }
            }
        }
        
        private void OnLeanChanged(PlayerAnimator obj)
        {
            if (!_configuration.AllowAcceptingWithKeys)
                return;

            var player = UnturnedPlayer.FromPlayer(obj.player);

            if (obj.lean == 1)
            {
                var request = _teleportRequests.Any(x => x.Item2.Equals(player) || x.Item1.Equals(player));
                
                if(request)
                    Cancel(player);
            }
            else if (obj.lean == -1)
            {
                var request = _teleportRequests.Any(x => x.Item2.Equals(player));
                
                if(request)
                    Accept(player);
            }
        }

        private void UpdateCooldown(UnturnedPlayer player)
        {
            if(_cooldowns.ContainsKey(player))
                _cooldowns[player] = DateTime.Now;
            else
                _cooldowns.Add(player, DateTime.Now);
        }

        private void UpdateLastCombat(UnturnedPlayer player)
        {
            if(_playersLastCombat.ContainsKey(player))
                _playersLastCombat[player] = DateTime.Now;
            else
                _playersLastCombat.Add(player, DateTime.Now);
        }

        private DateTime GetLastCombat(UnturnedPlayer player)
        {
            if (_playersLastCombat.ContainsKey(player))
                return _playersLastCombat[player];
            
            return DateTime.MinValue;
        }
        
        private void UpdateTeleportProtection(UnturnedPlayer player)
        {
            if (_teleportProtections.ContainsKey(player))
                _teleportProtections[player] = DateTime.Now;
            else 
                _teleportProtections.Add(player, DateTime.Now);
        }

        private DateTime GetTeleportProtection(UnturnedPlayer player)
        {
            if (_teleportProtections.ContainsKey(player))
                return _teleportProtections[player];
            
            return DateTime.MinValue;
        }
        
        private DateTime GetCooldown(UnturnedPlayer player)
        {
            if (_cooldowns.ContainsKey(player))
                return _cooldowns[player];
            
            return DateTime.MinValue;
        }
        
        private void Say(IRocketPlayer rocketPlayer, string message, Color messageColor, string icon = null)
        {
            var player = rocketPlayer as UnturnedPlayer;
            
            ChatManager.serverSendMessage(message, messageColor, toPlayer: player.SteamPlayer(), mode: EChatMode.SAY, iconURL: icon, useRichTextFormatting: true);
        }
        
        public void Dispose()
        {
            _teleportRequests = null;
            _teleportProtections = null;
            _cooldowns = null;
            _playersLastCombat = null;
            _plugin = null;
            _configuration = null;
            PlayerAnimator.OnLeanChanged_Global -= OnLeanChanged;
            DamageTool.onPlayerAllowedToDamagePlayer -= OnPlayerAllowedToDamagePlayer;
            U.Events.OnPlayerDisconnected -= OnLeave;
        }
    }
}