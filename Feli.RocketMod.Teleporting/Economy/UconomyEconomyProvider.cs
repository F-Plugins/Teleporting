using System;
using System.Linq;
using System.Reflection;

namespace Feli.RocketMod.Teleporting.Economy
{
    public class UconomyEconomyProvider : IEconomyProvider
    {
        private Assembly Uconomy
        {
            get
            {
                var uconomyAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name.Equals("Uconomy"));

                return uconomyAssembly;
            }
        }

        public void IncrementBalance(string playerId, decimal amount)
        {
            CallMethod("IncreaseBalance", new object[] { playerId, amount });
        }

        public decimal GetBalance(string playerId)
        {
            return (decimal) CallMethod("GetBalance", new object[] { playerId });
        }
        
        private object CallMethod(string name, object[] args)
        {
            if (Uconomy == null)
                throw new Exception("Uconomy was not found ! Make sure you have it install");

            var plugin = Uconomy.GetType("fr34kyn01535.Uconomy.Uconomy");

            var accessorField = plugin.GetField("Instance");
            var pluginInstance = accessorField.GetValue(null);

            var database = plugin.GetField("Database");
            var databaseInstance = database.GetValue(pluginInstance);   

            var method = database.FieldType.GetMethod(name);

            if (method == null)
            {
                throw new Exception("The plugin is trying to call a method that doesnt exist ! Contact the support");
            }
            
            return method.Invoke(databaseInstance, args);
        }
    }
}