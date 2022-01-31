using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Feli.OpenMod.Teleporting.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Ioc;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Players.Animator;
using OpenMod.Unturned.Players.Animator.Events;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;
using SDG.Unturned;
using SilK.Unturned.Extras.Events;
using Steamworks;
using UnityEngine;

namespace Feli.OpenMod.Teleporting.Services
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class TeleportsManager : ITeleportsManager, IInstanceAsyncEventListener<UnturnedPlayerLeanUpdatedEvent>, IInstanceEventListener<UnturnedPlayerDamagingEvent>, IInstanceEventListener<UnturnedUserDisconnectedEvent>, IDisposable
    {
        private List<Tuple<UnturnedUser, UnturnedUser>> _teleportRequests;
        private Dictionary<UnturnedUser, DateTime> _cooldowns;
        private Dictionary<UnturnedUser, DateTime> _teleportProtections;
        private Dictionary<UnturnedUser, DateTime> _playersLastCombat;

        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IUnturnedUserDirectory _unturnedUserDirectory;
        private readonly IServiceProvider _serviceProvider;
        private IEconomyProvider _economyProvider => _serviceProvider.GetRequiredService<IEconomyProvider>();

        public TeleportsManager(
            IServiceProvider serviceProvider,
            IUnturnedUserDirectory unturnedUserDirectory,
            IStringLocalizer stringLocalizer,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _unturnedUserDirectory = unturnedUserDirectory;
            _stringLocalizer = stringLocalizer;
            _configuration = configuration;
            _teleportRequests = new List<Tuple<UnturnedUser, UnturnedUser>>();
            _teleportProtections = new Dictionary<UnturnedUser, DateTime>();
            _playersLastCombat = new Dictionary<UnturnedUser, DateTime>();
            _cooldowns = new Dictionary<UnturnedUser, DateTime>();
        }
        
        public async Task Send(UnturnedUser sender, UnturnedUser target)
        {
            if (sender.Equals(target))
            {
                await Say(sender, _stringLocalizer["tpaCommand:send:yourself"]);
                return;
            }
            
            var request = _teleportRequests.FirstOrDefault(x => x.Item1.Equals(sender) && x.Item2.Equals(target));

            if (request != null)
            {
                _teleportRequests.Remove(request);
            }

            var cooldown = GetCooldown(sender);

            var cooldownTime = cooldown.AddSeconds(_configuration.GetSection("teleportOptions:cooldown").Get<double>());

            if (cooldownTime > DateTime.Now)
            {
                var waitTime = (cooldownTime - DateTime.Now).TotalSeconds;
                await Say(sender, _stringLocalizer["tpaCommand:send:cooldown", Math.Round(waitTime)]);
                return;
            }

            if (!_configuration.GetSection("teleportOptions:combat:allow").Get<bool>())
            {
                var combat = GetLastCombat(sender);

                var combatTime =
                    combat.AddSeconds(_configuration.GetSection("teleportOptions:combat:time").Get<double>());

                if (combatTime > DateTime.Now)
                {
                    var waitTime = (combatTime - DateTime.Now).TotalSeconds;
                    await Say(sender, _stringLocalizer["tpaCommand:send:combat", Math.Round(waitTime)]);
                    return;
                }
            }
            
            UpdateCooldown(sender);
            
            request = new Tuple<UnturnedUser, UnturnedUser>(sender, target);
            
            _teleportRequests.Add(request);

            if (_configuration.GetSection("tpaOptions:autoAcceptSameGroupRequests").Get<bool>() &&
                sender.Player.Player.quests.groupID != CSteamID.Nil &&
                target.Player.Player.quests.groupID != CSteamID.Nil &&
                sender.Player.Player.quests.groupID == target.Player.Player.quests.groupID)
            {
                await Accept(target, request);
                return;
            }
            
            await Say(sender, _stringLocalizer["tpaCommand:send:sender", target.DisplayName]);
            await Say(target, _stringLocalizer["tpaCommand:send:target", sender.DisplayName]);
        }

        public async Task Accept(UnturnedUser target, Tuple<UnturnedUser, UnturnedUser> request = null)
        {
            if (request == null)
            {
                request = _teleportRequests.FirstOrDefault(x => x.Item2.Equals(target));

                if (request == null)
                {
                    await Say(target, _stringLocalizer["tpaCommand:accept:noRequests"]);
                    return;
                }
            }

            var sender = request.Item1;

            await Say(target, _stringLocalizer["tpaCommand:accept:success", sender.DisplayName]);
            if (_configuration.GetSection("teleportOptions:delay").Get<double>() > 0)
            {
                await Say(sender,
                    _stringLocalizer["tpaCommand:accept:delay", target.DisplayName,
                        _configuration.GetSection("teleportOptions:delay").Get<double>()]);
            }

            var senderPosition = sender.Player.Player.transform.position;
            
            UniTask.Run(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_configuration.GetSection("teleportOptions:delay").Get<double>()));
                
                if (!_teleportRequests.Any(x => x.Item2.Equals(target) && x.Item1.Equals(sender)))
                    return;

                if (_configuration.GetSection("teleportOptions:cancelWhenMove").Get<bool>() &&
                    senderPosition != sender.Player.Player.transform.position)
                {
                    await Say(sender, _stringLocalizer["tpaValidation:move:sender"]);
                    await Say(target, _stringLocalizer["tpaValidation:move:target", sender.DisplayName]);
                    return;
                }
                
                var isValid = await ValidateRequest(request);

                if (!isValid)
                    return;

                if (_configuration.GetSection("teleportCost:enabled").Get<bool>())
                    await _economyProvider.UpdateBalanceAsync(sender.Id, KnownActorTypes.Player,
                        -_configuration.GetSection("teleportCost:cost").Get<decimal>(), "Teleport");
                
                UpdateTeleportProtection(sender);
                
                await UniTask.SwitchToMainThread();
                sender.Player.Player.teleportToLocationUnsafe(target.Player.Player.transform.position,
                    target.Player.Player.look.yaw);
                await UniTask.SwitchToThreadPool();

                _teleportRequests.Remove(request);
                await Say(sender, _stringLocalizer["tpaCommand:accept:teleported", target.DisplayName]);
            }).Forget();
        }

        public async Task List(UnturnedUser player)
        {
            var requests = _teleportRequests.Where(x => x.Item2.Equals(player));

            if (requests.Count() == 0)
            {
                await Say(player, _stringLocalizer["tpaCommand:list:notFound"]);
                return;
            }

            var playerNames = requests.Select(x => x.Item1.DisplayName);

            await Say(player, _stringLocalizer["tpaCommand:list:display", requests.Count()]);

            foreach (var name in playerNames)
            {
                await Say(player, _stringLocalizer["tpaCommand:list:section", name]);
            }
        }

        public async Task Cancel(UnturnedUser player)
        {
            var request = _teleportRequests.FirstOrDefault(x => x.Item1.Equals(player) || x.Item2.Equals(player));

            if (request == null)
            {
                await Say(player, _stringLocalizer["tpaCommand:cancel:noRequests"]);
                return;
            }
            
            var other = request.Item2.Equals(player) ? request.Item1 : request.Item2;

            _teleportRequests.Remove(request);

            await Say(player, _stringLocalizer["tpaCommand:cancel:success", other.DisplayName]);
            await Say(other, _stringLocalizer["tpaCommand:cancel:other", player.DisplayName]);
        }

        private async Task<bool> ValidateRequest(Tuple<UnturnedUser, UnturnedUser> request)
        {
            var sender = request.Item1;
            var target = request.Item2;

            if (sender.Player.CurrentVehicle != null || target.Player.CurrentVehicle != null)
            {
                var problem = sender.Player.CurrentVehicle != null ? sender : target;
                var noProblem = sender.Player.CurrentVehicle == null ? sender : target;


                await Say(problem, _stringLocalizer["tpaValidation:car:self"]);
                await Say(noProblem, _stringLocalizer["tpaValidation:car:self", problem.DisplayName]);
                
                return false;
            }

            if (!sender.Player.IsAlive || !target.Player.IsAlive)
            {
                var dead = !sender.Player.IsAlive ? sender : target;
                var alive = sender.Player.IsAlive ? sender : target;

                await Say(dead, _stringLocalizer["tpaValidation:dead:dead"]);
                await Say(alive, _stringLocalizer["tpaValidation:dead:alive", dead.DisplayName]);

                return false;
            }

            if (_configuration.GetSection("teleportCost:enabled").Get<bool>())
            {
                var balance = await _economyProvider.GetBalanceAsync(sender.Id, KnownActorTypes.Player);

                var cost = _configuration.GetSection("teleportCost:cost").Get<decimal>();
                
                if (balance < cost)
                {
                    await Say(sender, _stringLocalizer["tpaValidation:balance:sender", cost]);
                    await Say(target, _stringLocalizer["tpaValidation:balance:target", sender.DisplayName]);

                    return false;
                }
            }

            if (!_configuration.GetSection("tpaOptions:combat:allow").Get<bool>())
            {
                var senderCombat = GetLastCombat(sender);
                var targetCombat = GetLastCombat(target);

                var time = _configuration.GetSection("teleportOptions:combat:time").Get<double>();

                var senderCombatTime = senderCombat.AddSeconds(time);
                var targetCombatTime = targetCombat.AddSeconds(time);

                if (senderCombatTime > DateTime.Now)
                {
                    var waitTime = (senderCombatTime - DateTime.Now).TotalSeconds;

                    await Say(sender, _stringLocalizer["tpaValidation:combat:self", Math.Round(waitTime)]);
                    await Say(target, _stringLocalizer["tpaValidation:combat:other", sender.DisplayName]);

                    return false;
                }
                else if (targetCombatTime > DateTime.Now)
                {
                    var waitTime = (targetCombatTime - DateTime.Now).TotalSeconds;

                    await Say(target, _stringLocalizer["tpaValidation:combat:self", Math.Round(waitTime)]);
                    await Say(sender, _stringLocalizer["tpaValidation:combat:other", target.DisplayName]);

                    return false;
                }
            }
            
            return true;
        }
        
        private void UpdateCooldown(UnturnedUser player)
        {
            if(_cooldowns.ContainsKey(player))
                _cooldowns[player] = DateTime.Now;
            else
                _cooldowns.Add(player, DateTime.Now);
        }

        private void UpdateLastCombat(UnturnedUser player)
        {
            if(_playersLastCombat.ContainsKey(player))
                _playersLastCombat[player] = DateTime.Now;
            else
                _playersLastCombat.Add(player, DateTime.Now);
        }

        private DateTime GetLastCombat(UnturnedUser player)
        {
            if (_playersLastCombat.ContainsKey(player))
                return _playersLastCombat[player];
            
            return DateTime.MinValue;
        }
        
        private void UpdateTeleportProtection(UnturnedUser player)
        {
            if (_teleportProtections.ContainsKey(player))
                _teleportProtections[player] = DateTime.Now;
            else 
                _teleportProtections.Add(player, DateTime.Now);
        }

        private DateTime GetTeleportProtection(UnturnedUser player)
        {
            if (_teleportProtections.ContainsKey(player))
                return _teleportProtections[player];
            
            return DateTime.MinValue;
        }
        
        private DateTime GetCooldown(UnturnedUser player)
        {
            if (_cooldowns.ContainsKey(player))
                return _cooldowns[player];
            
            return DateTime.MinValue;
        }
        
        public async UniTask HandleEventAsync(object sender, UnturnedPlayerLeanUpdatedEvent @event)
        {
            if (_configuration.GetSection("teleportOptions:allowAcceptingWithKeys").Get<bool>())
                return;

            var user = _unturnedUserDirectory.FindUser(@event.Player.SteamId);

            if (@event.Lean == LeanType.Left)
            {
                var request = _teleportRequests.Any(x => x.Item2.Equals(user) || x.Item1.Equals(user));
                
                if(request)
                    await Cancel(user);
            }
            else if (@event.Lean == LeanType.Right)
            {
                var request = _teleportRequests.Any(x => x.Item2.Equals(user));
                
                if(request)
                    await Accept(user);
            }
        }

        public async UniTask HandleEventAsync(object sender, UnturnedPlayerDamagingEvent @event)
        {
            var instigator = _unturnedUserDirectory.FindUser(@event.Killer);
            
            if (instigator == null)
                return;
            
            UpdateLastCombat(instigator);

            if (!_configuration.GetSection("teleportOptions:teleportProtection:enabled").Get<bool>())
                return;
            
            var victim = _unturnedUserDirectory.FindUser(@event.Player.SteamId);

            if (!_teleportProtections.ContainsKey(victim))
                return;

            var teleportProtection = GetTeleportProtection(victim);

            var teleportProtectionTime = teleportProtection.AddSeconds(_configuration.GetSection("teleportOptions:teleportProtection:time").Get<double>());
            
            if (teleportProtection > DateTime.Now)
            {
                @event.IsCancelled = true;
                var waitTime = (teleportProtectionTime - DateTime.Now).TotalSeconds;

                await Say(instigator, _stringLocalizer["tpaProtection", Math.Round(waitTime), victim.DisplayName]);
            }
            else
            {
                _teleportProtections.Remove(victim);
            }
        }
        
        public async UniTask HandleEventAsync(object sender, UnturnedUserDisconnectedEvent @event)
        {
            var user = @event.User;
            
            var requests = _teleportRequests.Where(x => x.Item1.Equals(user) | x.Item2.Equals(user));

            foreach (var request in requests.ToList())
            {
                var other = request.Item1.Equals(user) ? request.Item2 : request.Item1;

                await Say(other, _stringLocalizer["tpaValidation:leave", user.DisplayName]);
                _teleportRequests.Remove(request);
            }

            var cooldown = GetCooldown(user);

            if (cooldown != DateTime.MinValue)
                _cooldowns.Remove(user);

            var protection = GetTeleportProtection(user);

            if (protection != DateTime.MinValue)
                _teleportProtections.Remove(user);

            var lastCombat = GetLastCombat(user);

            if (lastCombat != DateTime.MinValue)
                _playersLastCombat.Remove(user);        
        }
        
        private async Task Say(UnturnedUser user, string message)
        {
            await UniTask.SwitchToMainThread();
            ChatManager.serverSendMessage(message, Color.green, toPlayer: user.Player.SteamPlayer, mode: EChatMode.SAY,
                useRichTextFormatting: true, iconURL: _configuration["messages:icon"]);
            await UniTask.SwitchToThreadPool();
            //await user.PrintMessageAsync(message, Color.Green, true, _configuration["messages:icon"]);
        }

        public void Dispose()
        {
            _teleportRequests = null;
            _teleportProtections = null;
            _cooldowns = null;
            _playersLastCombat = null;
        }
    }
}