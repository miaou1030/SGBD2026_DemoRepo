using DemoRepo.Models;
using DemoRepo.Repositories;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");

Product product = new Product
{
    Id = 1,
    Name = "Sample Product",
    Price = 19.99m
};

Console.WriteLine(product.ToString());


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


//IProductRepository productRepository = new FakeProductRepository();
//IProductRepository productRepository = new AdoNetProductRepository();
//string connectionString = "Server=OUSSAMA\\SQL2026;Database=SGBD2026;Integrated Security=True;TrustServerCertificate=True;";
//IProductRepository productRepository = ProductRepoFactory.CreateProductRepository(RepositoryType.Fake);
//IProductRepository productRepository = ProductRepoFactory.CreateProductRepository(RepositoryType.AdoNet, connectionString);

string? connectionString = configuration.GetConnectionString("DefaultConnection");
string repositoryTypeString = configuration.GetSection("Repository:Type").Value ?? throw new InvalidOperationException("Repository type not found.");

RepositoryType repositoryType = Enum.Parse<RepositoryType>(repositoryTypeString, ignoreCase: true);

IProductRepository productRepository = ProductRepoFactory.CreateProductRepository(repositoryType, connectionString);

IManagerRepository managerRepository = ManagerRepoFactory.CreateManagerRepository(repositoryType, connectionString);

Console.WriteLine("--- Managers ---");
managerRepository.GetAllManagers().ToList().ForEach(m => Console.WriteLine(m));

// Ajout (prénom null : autorisé)
int newManagerId = managerRepository.AddManager(new Manager { MNG_FirstName = null, MNG_LastName = "ConsoleDemo" });
Console.WriteLine($"Manager ajouté, Id : {newManagerId}");

// Recherche par Id
Manager? foundManager = managerRepository.GetManagerById(newManagerId);
Console.WriteLine(foundManager?.ToString() ?? "Manager introuvable.");

if (foundManager != null)
{
    // Mise à jour
    foundManager.MNG_FirstName = "Demo";
    Console.WriteLine($"Mise à jour : {managerRepository.UpdateManager(foundManager)}");
    Console.WriteLine(managerRepository.GetManagerById(newManagerId));

    // Suppression
    Console.WriteLine($"Suppression : {managerRepository.DeleteManager(newManagerId)}");
}

Console.WriteLine("--- Fin Managers ---");

productRepository.GetAllProducts().ToList().ForEach(p => Console.WriteLine(p.ToString()));


IEnumerable<Product> products = productRepository.GetAllProducts();

foreach (var p in products)
{
    Console.WriteLine(p.ToString());
}


Product? productById = productRepository.GetProductById(2);
if (productById != null)
{
    Console.WriteLine(productById.ToString());
}
else
{
    Console.WriteLine("Product not found.");
}

Product newProduct = new Product
{
    Id = 4,
    Name = "New Product",
    Price = 29.99m
};

int newProductId = productRepository.AddProduct(newProduct);

Console.WriteLine($"Added new product with ID: {newProductId}");
productRepository.GetAllProducts().ToList().ForEach(p => Console.WriteLine(p.ToString()));

var foundProduct = productRepository.GetProductById(newProduct.Id);

if (foundProduct != null)
{
    foundProduct.Name = "Updated Product";
    var result = productRepository.UpdateProduct(foundProduct);
    if (result)
    {
        Console.WriteLine($"Updated product: {foundProduct.ToString()}");
        productRepository.GetAllProducts().ToList().ForEach(p => Console.WriteLine(p.ToString()));
    }
    else
    {
        Console.WriteLine("Failed to update product.");
    }

    Console.WriteLine($"Deleting product with ID: {foundProduct.Id}");
    productRepository.DeleteProduct(foundProduct.Id);

    productRepository.GetAllProducts().ToList().ForEach(p => Console.WriteLine(p.ToString()));

    Console.WriteLine($"C'est fni !");
    Console.WriteLine($"C'est fni 2 !");
}
else
{
    Console.WriteLine("Failed to add product.");
}

//******* Partie Manager *********

