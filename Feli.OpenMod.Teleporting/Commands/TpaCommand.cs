using System;
using System.Drawing;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Feli.OpenMod.Teleporting.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Color = UnityEngine.Color;

namespace Feli.OpenMod.Teleporting.Commands
{
    [Command("tpa")]
    [CommandDescription("Send, accept, deny and cancel teleport requests")]
    [CommandSyntax("<playerName|accept|send|cancel|list>")]
    [CommandAlias("tpr")]
    [CommandActor(typeof(UnturnedUser))]
    public class TpaCommand : UnturnedCommand
    {
        private readonly ITeleportsManager _teleportsManager;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IUnturnedUserDirectory _unturnedUserDirectory;
        
        public TpaCommand(
            IUnturnedUserDirectory unturnedUserDirectory,
            IStringLocalizer stringLocalizer,
            ITeleportsManager teleportsManager,
            IConfiguration configuration,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _unturnedUserDirectory = unturnedUserDirectory;
            _stringLocalizer = stringLocalizer;
            _teleportsManager = teleportsManager;
            _configuration = configuration;
        }

        protected override async UniTask OnExecuteAsync()
        {
            var user = Context.Actor as UnturnedUser;

            if (Context.Parameters.Length < 1)
            {
                await Say(user, _stringLocalizer["tpaCommand:wrongUsage:usage"]);
                return;
            }

            var type = Context.Parameters.ToArray()[0].ToLower();
            
            if (type == "s" | type == "send")
            {
                if (Context.Parameters.Length < 2)
                {
                    await Say(user, _stringLocalizer["tpaCommand:wrongUsage:send"]);
                    return;
                }

                var target =
                    _unturnedUserDirectory.FindUser(Context.Parameters.ToArray()[1], UserSearchMode.FindByName);

                if (target == null)
                {
                    await Say(user, _stringLocalizer["tpaCommand:wrongUsage:notFound"]);
                    return;
                }
                
                await _teleportsManager.Send(user, target);
            }
            else if (type == "a" | type == "accept")
                await _teleportsManager.Accept(user);
            else if (type == "c" | type == "cancel")
                await _teleportsManager.Cancel(user);
            else if (type == "l" | type == "list")
                await _teleportsManager.List(user);
            else
            {
                var target =
                    _unturnedUserDirectory.FindUser(Context.Parameters.ToArray()[0], UserSearchMode.FindByName);

                if (target == null)
                {
                    await Say(user, _stringLocalizer["tpaCommand:wrongUsage:notFound"]);
                    return;
                }
                
                await _teleportsManager.Send(user, target);
            }
        }
        
        private async UniTask Say(UnturnedUser user, string message)
        {
            await UniTask.SwitchToMainThread();
            ChatManager.serverSendMessage(message, Color.green, toPlayer: user.Player.SteamPlayer, mode: EChatMode.SAY,
                useRichTextFormatting: true, iconURL: _configuration["messages:icon"]);
            await UniTask.SwitchToThreadPool();
            //await user.PrintMessageAsync(message, Color.Green, true, _configuration["messages:icon"]);
        }
    }
}