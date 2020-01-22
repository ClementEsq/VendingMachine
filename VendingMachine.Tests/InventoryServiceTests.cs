using Bank.Consortium.Interfaces;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VendingMachine.Models;
using VendingMachine.Models.Enums;
using VendingMachine.Services;
using VendingMachine.Services.VendingMachine.Services.Interfaces;

namespace VendingMachine.Tests.InventoryTests
{

    [TestFixture, Category("Unit test")]
    public class InventoryServiceTests
    {
        private IInventoryService _inventoryService;
        private Mock<IVendinMachineDataBase> _vendinMachineDataBaseMock;

        IReadOnlyList<Drink> drinks = new List<Drink>
        {
            new Drink { Id = 1, DrinkType = DrinkType.soft, Price = 0.50 }
        };


        [SetUp]
        public void SetUp()
        {
            _vendinMachineDataBaseMock = new Mock<IVendinMachineDataBase>();
            _inventoryService = new InventoryService(_vendinMachineDataBaseMock.Object);

        }

        [Test]
        public void GetStockAsString_EnsureStockAvailabiltyDisplayStringIsInCorrectFormat()
        {

            StringBuilder stock = new StringBuilder();
            stock.AppendLine($"Drink Id: {drinks.ElementAt(0).Id} - Price: £{drinks.ElementAt(0).Price.ToString("0.00")} - Type: {drinks.ElementAt(0).DrinkType.ToString()}");
            var expectedStock = stock.ToString();

            _vendinMachineDataBaseMock.Setup(vdb => vdb.GetDrinks()).Returns(drinks);

            var actualStock = _inventoryService.GetStockAsString();

            Assert.That(expectedStock.Equals(actualStock));
        }

        [Test]
        public void IsDrinksAvailable_ReturnsTrueWhenDrinksAreAvailable()
        {
            _vendinMachineDataBaseMock.Setup(vdb => vdb.GetStockCount()).Returns(drinks.Count());

            var actualAvailability = _inventoryService.IsDrinksAvailable();

            Assert.IsTrue(actualAvailability);
        }

        [Test]
        public void IsDrinksAvailable_ReturnsFalseWhenDrinksNotAvailable()
        {
            _vendinMachineDataBaseMock.Setup(vdb => vdb.GetStockCount()).Returns(0);

            var actualAvailability = _inventoryService.IsDrinksAvailable();

            Assert.IsFalse(actualAvailability);
        }
    }
}