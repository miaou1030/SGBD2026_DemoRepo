using DemoRepo.Models;
using DemoRepo.Repositories;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MsSql;

namespace TestProjectDemo
{
    public class AdoNetProductRepositoryTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _container = new MsSqlBuilder().Build();
        private string _cs = null!;
        private IProductRepository _repo = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            //var cs =_cs = _container.GetConnectionString();

            await using (var master = new SqlConnection(_container.GetConnectionString()))
            {
                await master.OpenAsync();
                await new SqlCommand("CREATE DATABASE SGBD2026", master).ExecuteNonQueryAsync();
            }

            var cs = _cs = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "SGBD2026"
            }.ConnectionString;

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await new SqlCommand(
                "CREATE TABLE Products (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(100), Price DECIMAL(18, 2), CONSTRAINT UQ_Products_Name UNIQUE (Name))", 
                conn).ExecuteNonQueryAsync();
            
            await new SqlCommand(
                """
                CREATE PROCEDURE [dbo].[Product_Delete] 
                    @Id INT 
                AS 
                BEGIN 
                    DELETE FROM Products WHERE Id = @Id; 
                END
                """    
                   ,
                conn).ExecuteNonQueryAsync();

            _repo = ProductRepoFactory.CreateProductRepository(RepositoryType.AdoNet, cs);
        }

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();

        [Fact]
        public void AddProduct_ThenGetById_ReturnsProduct()
        {
            int id = _repo.AddProduct(new Product { Name = "Test", Price = 9.99m });

            var p = _repo.GetProductById(id);
            Assert.NotNull(p);
            Assert.Equal("Test", p.Name);
            Assert.Equal(9.99m, p.Price);
        }

        [Fact]
        public void AddProduct_WithDuplicateName_ThrowsUniqueViolation()
        {
            _repo.AddProduct(new Product { Name = "Unique", Price = 1.00m });
            
            var ex = Assert.Throws<SqlException>(
                () => _repo.AddProduct(new Product { Name = "Unique", Price = 2.00m }));
            
            Assert.Equal(2627, ex.Number); // Unique constraint violation error number
            Assert.Contains("UQ_Products_Name", ex.Message);
        }

        [Fact]
        public void DeleteProduct_Existing_ReturnsTrueAndremovesProduct()
        {
            int id = _repo.AddProduct(new Product { Name = "ToDelete", Price = 5.00m });
            
            bool deleted = _repo.DeleteProduct(id);

            Assert.True(deleted);
            var p = _repo.GetProductById(id);
            Assert.Null(p);
        }

        [Fact]
        public void DeleteProduct_Unknown_ReturnsFalse()
        {
            bool deleted = _repo.DeleteProduct(9999);
            
            Assert.False(deleted);
        }
    }
}
