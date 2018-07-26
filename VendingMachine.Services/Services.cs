using Bank.Consortium.Interfaces;
using Bank.Consortium.Service.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using User.Models;
using VendingMachine.Models;
using VendingMachine.Models.Enums;
using VendingMachine.Services.VendingMachine.Services.Interfaces;

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
            bool IsDrinksAvailable();
            Drink GetSingleDrinkFromInventory(int choice);
            string GetStockAsString();
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

        public string GetStockAsString()
        {
            var drinks = _vendinMachineDataBase.GetDrinks();
            StringBuilder stock = new StringBuilder();
            foreach (var drink in drinks)
            {
                stock.AppendLine($"Drink Id: {drink.Id} - Price: £{drink.Price.ToString("0.00")} - Type: {drink.DrinkType.ToString()}");

            }

            return stock.ToString();
        }

        public bool IsDrinksAvailable()
        {
            var stockCount = _vendinMachineDataBase.GetStockCount();
            return stockCount > 0;
        }

        public void StockVendingMachine(int quantityOfDrinks)
        {
            for (var i = 0; i < quantityOfDrinks; ++i)
            {
                _vendinMachineDataBase.Add(new Drink { Id = i + 1, DrinkType = DrinkType.soft, Price = 0.50 });
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
            IReadOnlyList<Drink> GetDrinks();
        }
    }

    namespace DataBase
    {
        namespace Bank
        {
            public class BankDataBase : IBankDataBase
            {
                private IReadOnlyList<Account> _accounts
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

                private IReadOnlyList<CardUserIdentity> _cardUserIdentities
                {
                    get
                    {
                        return new List<CardUserIdentity>()
                        {
                            new CardUserIdentity()
                            {
                                PIN = "1234",
                                Card = _accounts.FirstOrDefault(a => a.Id == 1)?.Cards?.FirstOrDefault(c => c.Id == 1)
                            },
                            new CardUserIdentity()
                            {
                                PIN = "2345",
                                Card = _accounts.FirstOrDefault(a => a.Id == 1)?.Cards?.FirstOrDefault(c => c.Id == 2)
                            }
                        };
                    }
                }

                public async Task<IReadOnlyList<Account>> GetAccounts()
                {
                    return await Task.Run(() => _accounts);
                }

                public async Task<IReadOnlyList<CardUserIdentity>> GetCardUserIdentities()
                {
                    return await Task.Run(() => _cardUserIdentities);
                }
            }
        }

        namespace VendingMachine
        {
            public class VendinMachineDataBase : IVendinMachineDataBase
            {
                private static List<Drink> _drinks;

                public VendinMachineDataBase()
                {
                    _drinks = new List<Drink>();
                }

                public void Add(Drink drink)
                {
                    _drinks.Add(drink);
                }

                public Drink Get(int choice)
                {
                    var drink = _drinks.FirstOrDefault(d => d.Id == choice);
                    Remove(drink, choice);

                    return drink;
                }

                public IReadOnlyList<Drink> GetDrinks()
                {
                    return _drinks;
                }

                public int GetStockCount()
                {
                    return _drinks.Count();
                }

                private void Remove(Drink drink, int id)
                {
                    if (drink != null)
                    {
                        for (var i = 0; i < _drinks.Count; ++i)
                        {
                            if (_drinks.ElementAt(i).Id == id)
                            {
                                _drinks.RemoveAt(i);
                                break;
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
            var id = cuids?.FirstOrDefault(c => c.PIN == suppliedPIN && c.Card?.Id == cardId);

            return id != null;
        }
    }

    public class AccountService : IAccountService
    {
        private readonly IBankDataBase _dataBase;
        private readonly object _balanceLock = new object();

        public AccountService(IBankDataBase dataBase)
        {
            _dataBase = dataBase;
        }

        public async Task DeductFromAccountBalance(int accountId, double amountCharged)
        {
            var accounts = await _dataBase.GetAccounts();
            var account = accounts?.FirstOrDefault(a => a.Id == accountId);

            if (account != null)
            {
                lock (_balanceLock)
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