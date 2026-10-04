using DemoRepo.Models;
using DemoRepo.Repositories;

Console.WriteLine("Hello, World!");

Product product = new Product
{
    Id = 1,
    Name = "Sample Product",
    Price = 19.99m
};

Console.WriteLine(product.ToString());


IProductRepository productRepository = new FakeProductRepository();

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
