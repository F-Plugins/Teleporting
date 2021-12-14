using System;
using SDG.Unturned;
using Steamworks;

namespace Feli.RocketMod.Teleporting.Economy
{
    public class ExperienceEconomyProvider : IEconomyProvider
    {
        public void IncrementBalance(string playerId, decimal amount)
        {
            var player = GetPlayerFromId(playerId);

            if (amount < 0)
            {
                player.skills.askSpend((uint)Math.Abs(amount));
            }
            else
            {
                player.skills.askAward((uint)amount);
            }
        }

        public decimal GetBalance(string playerId)
        {
            var player = GetPlayerFromId(playerId);

            return player.skills.experience;
        }

        private Player GetPlayerFromId(string playerId)
        {
            var player = PlayerTool.getPlayer(new CSteamID(ulong.Parse(playerId)));

            if (player == null)
                throw new Exception($"No player was found with id: {playerId}");
            
            return player;
        }
    }
}