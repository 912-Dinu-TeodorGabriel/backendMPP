using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Bogus;

namespace BackEnd
{
	public class ProductService
	{
		private readonly string _connectionString;
		private ReviewService _reviewService;

		public ProductService(string connectionString)
		{
			_connectionString = connectionString;
			_reviewService = new ReviewService(connectionString);
			
		}

		public List<Product> GenerateFakeProducts(int count)
		{
			var faker = new Faker<Product>()
				.RuleFor(p => p.id, f => -1)
				.RuleFor(p => p.type, f => f.PickRandom("Laptop", "Smartphone", "Tablet", "Desktop"))
				.RuleFor(p => p.productName, f => f.Commerce.ProductName())
				.RuleFor(p => p.image, f => $"{f.Commerce.ProductName().Replace(" ", "-")}.jpg")
				.RuleFor(p => p.rating, "0")
				.RuleFor(p => p.price, f => f.Random.Number(500, 3000).ToString())
				.RuleFor(p => p.cpu, f => f.PickRandom("Intel Core i7", "Intel Core i9", "AMD Ryzen 5", "AMD Ryzen 7", "Apple M1", "Qualcomm Snapdragon 865"))
				.RuleFor(p => p.ram, f => f.PickRandom("8GB", "16GB", "32GB", "64GB"))
				.RuleFor(p => p.storage, f => f.PickRandom("256GB SSD", "512GB SSD", "1TB SSD", "2TB HDD"))
				.RuleFor(p => p.screen, f => f.PickRandom("13.3-inch", "15.6-inch", "6.7-inch", "10.2-inch"))
				.RuleFor(p => p.resolution, f => f.PickRandom("1920x1080", "2560x1440", "2732x2048", "3840x2160"))
				.RuleFor(p => p.displayTechnology, f => f.PickRandom("IPS", "AMOLED", "Retina", "LED"))
				.RuleFor(p => p.description, f => f.Lorem.Paragraph());

			return faker.Generate(count);
		}
		public async Task GenerateAndInsertProductsAsync()
		{

			var products = GenerateFakeProducts(100000);

			foreach (var product in products)
			{
				 await WriteProduct(product);
			}

		}
		public List<Product> ReadProduct(string type, int offset, int limit)
		{
			var products = new List<Product>();
			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.Open();
					var query = string.Empty;
					if (type == "all")
					{
						query = "SELECT * FROM Products ORDER BY id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
					}
					else
					{
						query = "SELECT * FROM Products WHERE type = @type ORDER BY id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
					}

					using (var command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@type", type);
						command.Parameters.AddWithValue("@offset", offset);
						command.Parameters.AddWithValue("@limit", limit);
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								products.Add(new Product
								{
									id = Convert.ToInt32(reader["id"]),
									type = reader["type"].ToString(),
									productName = reader["productName"].ToString(),
									image = reader["image"].ToString(),
									rating = _reviewService.GetAverageRating(Convert.ToInt32(reader["id"])).ToString(),
									price = reader["price"].ToString(),
									cpu = reader["cpu"].ToString(),
									ram = reader["ram"].ToString(),
									storage = reader["storage"].ToString(),
									screen = reader["screen"].ToString(),
									resolution = reader["resolution"].ToString(),
									displayTechnology = reader["displayTechnology"].ToString(),
									description = reader["description"].ToString()
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error reading Products: {ex.Message}");
			}

			return products;
		}

		public IResult SpecificProduct(int id)
		{
			Product product = null;
			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.Open();
					var query = "SELECT * FROM Products WHERE id = @id";
					using (var command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@id", id);
						using (var reader = command.ExecuteReader())
						{
							if (reader.Read())
							{
								product = new Product
								{
									id = Convert.ToInt32(reader["id"]),
									type = reader["type"].ToString(),
									productName = reader["productName"].ToString(),
									image = reader["image"].ToString(),
									rating = _reviewService.GetAverageRating(id).ToString(),
									price = reader["price"].ToString(),
									cpu = reader["cpu"].ToString(),
									ram = reader["ram"].ToString(),
									storage = reader["storage"].ToString(),
									screen = reader["screen"].ToString(),
									resolution = reader["resolution"].ToString(),
									displayTechnology = reader["displayTechnology"].ToString(),
									description = reader["description"].ToString()
								};
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error reading Product: {ex.Message}");
			}

			return Results.Json(product);
		}

		// Implement other methods similarly

	public async Task<IResult> WriteProduct(Product newProduct)
	{
		if (newProduct == null)
		{
			return Results.BadRequest("Invalid input");
		}
		try
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync();
				var query = string.Empty;
				if (newProduct.id == -1)
				{
					// Insert new product
					query = @"INSERT INTO Products (type, productName, image, price, cpu, ram, storage, screen, resolution, displayTechnology, description)
							  VALUES (@type, @productName, @image, @price, @cpu, @ram, @storage, @screen, @resolution, @displayTechnology, @description)";
				}
				else
				{
					// Update existing product
					query = @"UPDATE Products 
							  SET type = @type, productName = @productName, image = @image, price = @price, cpu = @cpu, ram = @ram, 
								  storage = @storage, screen = @screen, resolution = @resolution, displayTechnology = @displayTechnology, description = @description
							  WHERE id = @id";
				}

				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@type", newProduct.type ?? "Laptop");
					command.Parameters.AddWithValue("@productName", newProduct.productName ?? "Unknown");
					command.Parameters.AddWithValue("@image", newProduct.image ?? "Unknown");
					command.Parameters.AddWithValue("@price", newProduct.price ?? "0");
					command.Parameters.AddWithValue("@cpu", newProduct.cpu ?? "Unknown");
					command.Parameters.AddWithValue("@ram", newProduct.ram ?? "Unknown");
					command.Parameters.AddWithValue("@storage", newProduct.storage ?? "Unknown");
					command.Parameters.AddWithValue("@screen", newProduct.screen ?? "Unknown");
					command.Parameters.AddWithValue("@resolution", newProduct.resolution ?? "Unknown");
					command.Parameters.AddWithValue("@displayTechnology", newProduct.displayTechnology ?? "Unknown");
					command.Parameters.AddWithValue("@description", newProduct.description ?? "Unknown");
					if (newProduct.id != 0)
					{
						command.Parameters.AddWithValue("@id", newProduct.id);
					}

					await command.ExecuteNonQueryAsync();
				}
			}

			return Results.Ok("Product added/updated successfully");
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error writing Product: {ex.Message}");
			return Results.Problem("Error writing Product");
		}
	}

		public IResult DeleteProduct(int id)
		{
			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.Open();
					var query = "DELETE FROM Products WHERE id = @id";
					using (var command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@id", id);
						var rowsAffected = command.ExecuteNonQuery();
						if (rowsAffected == 0)
						{
							Console.Error.WriteLine($"Item with id '{id}' not found");
							return Results.NotFound("Item not found");
						}
					}
				}

				Console.WriteLine($"Item with id '{id}' deleted");
				return Results.Ok("Item deleted successfully");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error deleting Product: {ex.Message}");
				return Results.Problem("Error deleting Product");
			}
		}
		public IResult GetPaginatedProduct(string type, HttpRequest request)
		{
			int offset = request.Query.ContainsKey("offset") ? Convert.ToInt32(request.Query["offset"]) : 0;
			int limit = request.Query.ContainsKey("limit") ? Convert.ToInt32(request.Query["limit"]) : 10;
			
			var products = ReadProduct(type, offset, limit);
			return Results.Json(products);
			
		}
		//if i have just new in productName i want everything that contains new
		public List<Product> GetProductsByName(string productName)
		{
			var products = new List<Product>();
			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.Open();
					var query = "SELECT * FROM Products WHERE productName LIKE @productName";
					using (var command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@productName", $"%{productName}%");
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								products.Add(new Product
								{
									id = Convert.ToInt32(reader["id"]),
									type = reader["type"].ToString(),
									productName = reader["productName"].ToString(),
									image = reader["image"].ToString(),
									rating = _reviewService.GetAverageRating(Convert.ToInt32(reader["id"])).ToString(),
									price = reader["price"].ToString(),
									cpu = reader["cpu"].ToString(),
									ram = reader["ram"].ToString(),
									storage = reader["storage"].ToString(),
									screen = reader["screen"].ToString(),
									resolution = reader["resolution"].ToString(),
									displayTechnology = reader["displayTechnology"].ToString(),
									description = reader["description"].ToString()
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error reading Products: {ex.Message}");
			}
			return products;
		}
	}
	
}
