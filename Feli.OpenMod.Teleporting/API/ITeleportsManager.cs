using System;
using System.Threading.Tasks;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;

namespace Feli.OpenMod.Teleporting.API
{
    [Service]
    public interface ITeleportsManager
    {
        Task Send(UnturnedUser sender, UnturnedUser target);
        Task Accept(UnturnedUser target, Tuple<UnturnedUser, UnturnedUser> request = null);
        Task List(UnturnedUser player);
        Task Cancel(UnturnedUser player);
    }
}