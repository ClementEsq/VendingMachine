using System;
using System.Collections.Generic;
using System.Linq;
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
using VendingMachine.Models.Enums;
using VendingMachine.Services;
using VendingMachine.Services.VendingMachine.Services.Interfaces;

namespace VendingMachine
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Application application = new Application(serviceCollection);

            var cardValidationService = application.CreateCardValidationService();
            var affordabilityService = application.CreateAffordabilityService();
            var inventoryService = application.CreateInventoryService();
            var purchaseService = application.CreatePurchaseService();

            var PIN = Console.ReadLine();

            Card card = new Card();
            UserCardEntry userCardEntry = new UserCardEntry();

            Console.ReadKey();        
        }

        private UserCardEntry CreateUserCardEntry(string PIN)
        {
            return new UserCardEntry
            {
                Card = CreateCard(),
                PIN
            };                
        }


        private Card CreateCard()
        {
            return new Card
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
            Logger.LogInformation($"creating validation service");
            var cardValidationService = Services.GetRequiredService<ICardValidationService>();

            return cardValidationService;
        }

        public IAffordabilityService CreateAffordabilityService()
        {
            Logger.LogInformation($"creating validation service");
            var affordabilityService = Services.GetRequiredService<IAffordabilityService>();

            return affordabilityService;
        }

        public IPurchaseService CreatePurchaseService()
        {
            Logger.LogInformation($"creating purchase service");
            var purchaseService = Services.GetRequiredService<IPurchaseService>();

            return purchaseService;
        }

        public IInventoryService CreateInventoryService()
        {
            Logger.LogInformation($"creating inventory service");
            var inventoryService = Services.GetRequiredService<IInventoryService>();

            return inventoryService;
        }
    }
}

namespace VendingMachine.Models
{
    namespace Enums
    {
        public enum DrinkType
        {
            soft
        }
    }

    public class Drink
    {
        public int Id { get; set; }
        public DrinkType DrinkType { get; set; }
        public double Price { get; set; }
    }

    public class UserCardEntry
    {
        public Card Card { get; set; }
        public string SuppliedPIN { get; set; }
    }
}

namespace VendingMachine.Services
{
    namespace VendingMachine.Services.Interfaces
    {
        public interface ICardValidationService
        {
            Task<bool> IsSuppliedCredentialsValid(UserCardEntry userCardEntry);
        }

        public interface IAffordabilityService
        {
            Task<bool> IsBuyerAboveMinimumAcountBalance(int accountId);
        }

        public interface IPurchaseService
        {

            Task CompletePurchase(int accountId, double amountCharged);
        }

        public interface IInventoryService
        {
            void StockVendingMachine(int quantityOfDrinks);
            bool IsStockEmpty();
            Drink GetSingleDrinkFromInventory(int choice);
        }
    }

    public class CardValidationService : ICardValidationService
    {
        private readonly ICardUserIdentityService _cardUserIdentityService;

        public CardValidationService(ICardUserIdentityService cardUserIdentityService)
        {
            _cardUserIdentityService = cardUserIdentityService;
        }

        public async Task<bool> IsSuppliedCredentialsValid(UserCardEntry userCardEntry)
        {
            var isValid = await _cardUserIdentityService.AuthenticateCard(userCardEntry.Card.Id, userCardEntry.SuppliedPIN);
            return isValid;
        }
    }

    public class AffordabilityService : IAffordabilityService
    {
        private readonly IAccountService _accountService;

        public AffordabilityService(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<bool> IsBuyerAboveMinimumAcountBalance(int accountId)
        {
            //magic number minimum needs to go to config file
            var isAboveMinimum = await _accountService.IsUserDebitAboveMinimumAccountBalance(accountId, 0.50);
            return isAboveMinimum;
        }
    }

    public class PurchaseService : IPurchaseService
    {
        private readonly IAccountService _accountService;

        public PurchaseService(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task CompletePurchase(int accountId, double priceOfDrink)
        {
            await _accountService.DeductFromAccountBalance(accountId, priceOfDrink);
        }
    }

    public class InventoryService : IInventoryService
    {
        private readonly IVendinMachineDataBase _vendinMachineDataBase;

        public InventoryService(IVendinMachineDataBase vendinMachineDataBase)
        {
            _vendinMachineDataBase = vendinMachineDataBase;
        }

        public Drink GetSingleDrinkFromInventory(int choice)
        {
            return _vendinMachineDataBase.Get(choice);
        }

        public bool IsStockEmpty()
        {
            var stockCount = _vendinMachineDataBase.GetStockCount();
            return stockCount > 0;
        }

        public void StockVendingMachine(int quantityOfDrinks)
        {
            for (var i = 0; i < quantityOfDrinks; ++i)
            {
                _vendinMachineDataBase.Add(new Drink { DrinkType = DrinkType.soft, Price = 0.50 });
            }
        }
    }
}


namespace User.Models
{
    public class Card
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
    }

    public class CashCard : Card
    {
    }

    public class CardUserIdentity
    {
        public string PIN { get; set; }
        public Card Card { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public double Balance { get; set; }
        public IReadOnlyList<Card> Cards { get; set; }
    }

    public class JointAccount : Account
    {       
    }
}

namespace Bank.Consortium
{
    namespace Interfaces
    {
        public interface IBankDataBase
        {
            Task<IReadOnlyList<Account>> GetAccounts();
            Task<IReadOnlyList<CardUserIdentity>> GetCardUserIdentities();
        }

        public interface IVendinMachineDataBase
        {
            void Add(Drink drink);
            int GetStockCount();
            Drink Get(int choice);
        }
    }

    namespace DataBase
    {
        namespace Bank
        {
            public class BankDataBase : IBankDataBase
            {
                private IReadOnlyList<Account> Accounts
                {
                    get
                    {
                        var accountId = 1;

                        return new List<Account>()
                    {
                        new JointAccount()
                        {
                            Id = accountId,
                            Balance = 100.00,
                            Cards = new List<Card>()
                            {
                                new CashCard()
                                {
                                    Id = 1,
                                    AccountId = accountId
                                },
                                new CashCard()
                                {
                                    Id = 2,
                                    AccountId = accountId
                                }
                            }
                        }
                    };
                    }
                }

                private IReadOnlyList<CardUserIdentity> CardUserIdentities
                {
                    get
                    {
                        return new List<CardUserIdentity>()
                        {
                            new CardUserIdentity()
                            {
                                PIN = "1234",
                                Card = Accounts.FirstOrDefault(a => a.Id == 1)?.Cards?.FirstOrDefault(c => c.Id == 1)
                            },
                            new CardUserIdentity()
                            {
                                PIN = "2345",
                                Card = Accounts.FirstOrDefault(a => a.Id == 1)?.Cards?.FirstOrDefault(c => c.Id == 2)
                            }
                        };
                    }
                }

                public async Task<IReadOnlyList<Account>> GetAccounts()
                {
                    return await Task.Run(() => Accounts);
                }

                public async Task<IReadOnlyList<CardUserIdentity>> GetCardUserIdentities()
                {
                    return await Task.Run(() => CardUserIdentities);
                }
            }
        }

        namespace VendingMachine
        {
            public class VendinMachineDataBase : IVendinMachineDataBase
            {
                private static List<Drink> Drinks;

                public VendinMachineDataBase()
                {
                    Drinks = new List<Drink>();
                }

                public void Add(Drink drink)
                {
                    Drinks.Add(drink);
                }

                public Drink Get(int choice)
                {
                    var drink = Drinks.FirstOrDefault(d => d.Id == choice);
                    Remove(drink, choice);

                    return drink;
                }

                public int GetStockCount()
                {
                    return Drinks.Count();
                }

                private void Remove(Drink drink, int id)
                {
                    if (drink != null)
                    {
                        for (var i = 0; i < Drinks.Count; ++i)
                        {
                            if (Drinks.ElementAt(i).Id == id)
                            {
                                Drinks.RemoveAt(i);
                            }
                        }
                    }
                }
            }
  

        }
    }

    namespace Service.Interface
    {
        public interface ICardUserIdentityService
        {
            Task<bool> AuthenticateCard(int cardId, string suppliedPIN);
        }

        public interface IAccountService
        {
            Task<bool> IsUserDebitAboveMinimumAccountBalance(int accountId, double minimumAccountBalance);
            Task DeductFromAccountBalance(int accountId, double amountCharged);
        }
    }

    public class CardUserIdentityService : ICardUserIdentityService
    {
        private readonly IBankDataBase _dataBase;

        public CardUserIdentityService(IBankDataBase dataBase)
        {
            _dataBase = dataBase;
        }

        public async Task<bool> AuthenticateCard(int cardId, string suppliedPIN)
        {
            var cuids = await _dataBase.GetCardUserIdentities();
            var cuid = cuids?.FirstOrDefault(c => c.PIN == suppliedPIN && c.Card?.Id == cardId);

            return cuids != null;
        }
    }

    public class AccountService : IAccountService
    {
        private readonly IBankDataBase _dataBase;
        private readonly object balanceLock;

        public AccountService(IBankDataBase dataBase)
        {
            _dataBase = dataBase;
        }

        public async Task DeductFromAccountBalance(int accountId, double amountCharged)
        {
            var accounts = await _dataBase.GetAccounts();
            var account = accounts?.FirstOrDefault(a => a.Id == accountId);

            if(account != null)
            {
                lock(balanceLock)
                {
                    account.Balance -= amountCharged;
                }
            }
        }

        public async Task<bool> IsUserDebitAboveMinimumAccountBalance(int accountId, double minimumAccountBalance)
        {
            var accounts = await _dataBase.GetAccounts();
            var account = accounts?.FirstOrDefault(a => a.Id == accountId);
            var isAccountBalanceBelowMinimum = account?.Balance > minimumAccountBalance;
            return isAccountBalanceBelowMinimum;
        }
    }
}