using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bank.Consortium;
using Bank.Consortium.DataBase.Bank;
using Bank.Consortium.DataBase.VendingMachine;
using Bank.Consortium.Interfaces;
using Bank.Consortium.Service.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User.Models;
using VendingMachine.Models;
using VendingMachine.Services;
using VendingMachine.Services.VendingMachine.Services.Interfaces;

namespace VendingMachine
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Application application = new Application(serviceCollection);

            var cardValidationService = application.CreateCardValidationService();
            var affordabilityService = application.CreateAffordabilityService();
            var inventoryService = application.CreateInventoryService();
            var purchaseService = application.CreatePurchaseService();

            inventoryService.StockVendingMachine(25);

            do
            {
                try
                {
                    //will have to break this section down into smaller functions that only have one responsibility
                    if (inventoryService.IsDrinksAvailable())
                    {
                        var availableDrinks = inventoryService.GetStockAsString();

                        application.LogToConsole("Available drinks:");

                        application.LogToConsole(availableDrinks);

                        application.LogToConsole("Please enter pin:");
                        var PIN = application.ReadUserEntry();
                        var entry = CreateUserCardEntry(PIN);

                        var isValid = await cardValidationService.IsSuppliedCredentialsValid(entry);

                        var canBuy = isValid ? await affordabilityService.IsBuyerAboveMinimumAcountBalance(entry.Card.AccountId) : false;

                        if (canBuy)
                        {
                            application.LogToConsole("Please enter one of the drink IDs shown above:");
                            var choice = application.ReadUserEntry();
                            var isValidNumber = int.TryParse(choice, out int intChoice);

                            var drink = isValidNumber ? inventoryService.GetSingleDrinkFromInventory(intChoice) : null;

                            if (drink != null)
                            {
                                await purchaseService.CompletePurchase(entry.Card.AccountId, drink.Price);
                                application.LogToConsole($"You've bought {drink.Id}");

                            }
                            else
                            {
                                application.LogToConsole($"{choice} is an invalid selection");
                            }
                        }
                        else
                        {
                            application.LogToConsole("Invalid credentails");
                        }
                    }
                    else
                    {
                        application.LogToConsole("Replenishing inventory");
                        inventoryService.StockVendingMachine(25);
                    }
                }
                catch (Exception)
                {
                    application.LogToConsole("Error Occured!");
                }
            } while (true);
        }


        private static UserCardEntry CreateUserCardEntry(string PIN)
        {
            return new UserCardEntry
            {
                Card = CreateCard(),
                SuppliedPIN = PIN
            };
        }


        private static UserCard CreateCard()
        {
            return new UserCard
            {
                Id = 1,
                AccountId = 1
            };
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging();
        }
    }

    public class Application
    {
        public IServiceProvider Services { get; set; }
        public ILogger Logger { get; set; }
        public Application(IServiceCollection serviceCollection)
        {
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
            Logger = Services.GetRequiredService<ILoggerFactory>().AddConsole().CreateLogger<Application>();

            Logger.LogInformation("Application created successfully.");

        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            ConfigureVendingMachineServices(serviceCollection);
            ConfigureVendingMachineDataStore(serviceCollection);
            ConfigureBankServices(serviceCollection);
            ConfigureBankDataStore(serviceCollection);
        }

        private void ConfigureVendingMachineServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ICardValidationService, CardValidationService>();
            serviceCollection.AddTransient<IAffordabilityService, AffordabilityService>();
            serviceCollection.AddTransient<IPurchaseService, PurchaseService>();
            serviceCollection.AddTransient<IInventoryService, InventoryService>();
        }

        private void ConfigureVendingMachineDataStore(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IVendinMachineDataBase, VendinMachineDataBase>();
        }

        private void ConfigureBankServices(IServiceCollection serviceCollection)
        {

            serviceCollection.AddTransient<ICardUserIdentityService, CardUserIdentityService>();
            serviceCollection.AddTransient<IAccountService, AccountService>();
        }

        private void ConfigureBankDataStore(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IBankDataBase, BankDataBase>();
        }

        public ICardValidationService CreateCardValidationService()
        {
            var cardValidationService = Services.GetRequiredService<ICardValidationService>();

            return cardValidationService;
        }

        public IAffordabilityService CreateAffordabilityService()
        {
            var affordabilityService = Services.GetRequiredService<IAffordabilityService>();

            return affordabilityService;
        }

        public IPurchaseService CreatePurchaseService()
        {
            var purchaseService = Services.GetRequiredService<IPurchaseService>();

            return purchaseService;
        }

        public IInventoryService CreateInventoryService()
        {
            var inventoryService = Services.GetRequiredService<IInventoryService>();

            return inventoryService;
        }

        public void LogToConsole(string message)
        {
            Logger.LogInformation(message);
        }

        public string ReadUserEntry()
        {
            return Console.ReadLine();
        }
    }
}