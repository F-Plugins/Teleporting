namespace Feli.RocketMod.Teleporting.Economy
{
    public interface IEconomyProvider
    {
        void IncrementBalance(string playerId, decimal amount);
        decimal GetBalance(string playerId);
    }
}