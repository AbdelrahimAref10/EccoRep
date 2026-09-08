using Application.Features.Products.Query.GetProductList;
using CSharpFunctionalExtensions;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class ProductTest
    {
        private Mock<IMediator> _mediatorMock;
        private DatabaseContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _dbContext = new DatabaseContext(options);
            _mediatorMock = new Mock<IMediator>();

        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public void AddProduct_ShouldSaveProductToDatabase()
        {
            // Arrange
            var product = new Product { ProductId = 1, ProductName = "Test Product", Price = 50 };

            // Act
            _dbContext.Products.Add(product);
            _dbContext.SaveChanges();

            var savedProduct = _dbContext.Products.FirstOrDefault(p => p.ProductId == 1);

            _mediatorMock.Object.Send(new )
            // Assert
            Assert.That(savedProduct, Is.Not.Null);
            Assert.That(savedProduct.ProductName, Is.EqualTo("Test Product"));
            Assert.That(savedProduct.Price, Is.EqualTo(50));
        }


        [Test]
        public async Task GetProductsQuery_ShouldReturnSuccessResultWithListOfProducts()
        {
            // Arrange
            var query = new GetProductsQuery();

            var expectedProducts = new List<ProductsVm>
            {
                new ProductsVm { ProductId = 1, ProductName = "Test Product 1", ProductDescription = "Description 1", Price = 10, CategoryName = "Category 1" },
                new ProductsVm { ProductId = 2, ProductName = "Test Product 2", ProductDescription = "Description 2", Price = 20, CategoryName = "Category 2" }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Result.Success(expectedProducts));

            // Act
            var result = await _mediatorMock.Object.Send(query);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Count, Is.EqualTo(2));
            Assert.That(result.Value[0].ProductName, Is.EqualTo("Test Product 1"));
        }

        [Test]
        public async Task GetProductsQuery_ShouldReturnFailureResultWhenNoProductsFound()
        {
            // Arrange
            var query = new GetProductsQuery();

            _mediatorMock.Setup(m => m.Send(It.Is<GetProductsQuery>(q => q == query), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Result.Failure<List<ProductsVm>>("No Products Found"));

            // Act
            var result = await _mediatorMock.Object.Send(query);

            // Assert
            Assert.That(result.IsFailure, Is.True, "Expected result to be a failure but was successful.");
            Assert.That(result.Error, Is.EqualTo("No Products Found"));
        }
    }
}
