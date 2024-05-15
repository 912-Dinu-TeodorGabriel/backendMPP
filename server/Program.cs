using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BackEnd;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5125");
builder.Services.AddControllers();
builder.Services.AddCors();

var app = builder.Build();


app.UseWebSockets();
// Middleware to handle CORS
app.UseCors(options =>
{
	options.AllowAnyOrigin()
		   .AllowAnyMethod()
		   .AllowAnyHeader();
});

// Instantiate _productService
var _productService = new ProductService("Server=localhost;Database=MPP-project;User Id=SA;Password=Password123;TrustServerCertificate=true");
var _reviewService = new ReviewService("Server=localhost;Database=MPP-project;User Id=SA;Password=Password123;TrustServerCertificate=true");
var _userService = new UserService("Server=localhost;Database=MPP-project;User Id=SA;Password=Password123;TrustServerCertificate=true");
// Endpoint to get paginated data for a specific type
app.MapGet("/api/{type}", (string type, HttpRequest request) =>
{
	return _productService.GetPaginatedProduct(type, request);
});


app.MapGet("/api/product/{id}", (int id) =>
{
	return _productService.SpecificProduct(id);
});
app.MapGet("/api/review/{id}", (int id) =>
{
	return _reviewService.GetReviewsByProductId(id);
});
app.MapDelete("/api/review/{id}", (int id) =>
{
	return _reviewService.DeleteReviewAsync(id);
});
app.MapPost("/api/review/update", async (HttpRequest request) =>
{
	using (var reader = new StreamReader(request.Body))
	{
		var body = await reader.ReadToEndAsync();
		var review = JsonSerializer.Deserialize<Review>(body) ?? new Review();
		try{
			await _reviewService.UpdateReviewAsync(review);
			return Results.Ok("Review updated successfully");
		}
		catch(Exception ex){
			Console.Error.WriteLine($"Error updating review: {ex.Message}");
			return Results.Problem("Error updating review");
		}
	}
});
app.MapGet("/api/review/nr/{id}", (int id) =>
{
	return _reviewService.GetNumberOfReviewsByProductId(id);
});
app.MapPost("/api/review/add", async (HttpRequest request) =>
{
	using (var reader = new StreamReader(request.Body))
	{
		var body = await reader.ReadToEndAsync();
		Console.WriteLine(body);
		var review = JsonSerializer.Deserialize<Review>(body) ?? new Review();
		try{
			await ReviewService.AddReviewAsync(review);
			return Results.Ok("Review added successfully");
		}
		catch(Exception ex){
			Console.Error.WriteLine($"Error adding review: {ex.Message}");
			return Results.Problem("Error adding review");
		}
	}
});
// Endpoint to update data for a specific type
app.MapPost("/api/{id}", (int id, Product newData) =>
{
	return _productService.WriteProduct(newData);
});
app.MapGet("api/products/{name}",(string name) => 
{
	return _productService.GetProductsByName(name);
});
// Endpoint to delete data for a specific type by ID
app.MapDelete("/api/{id}", (int id) =>
{
	return _productService.DeleteProduct(id);
});

//User endpoints
app.MapPost("/api/user/login" , async (HttpRequest request) =>
{
	using (var reader = new StreamReader(request.Body))
	{
		var body = await reader.ReadToEndAsync();
		Console.WriteLine(body);
		var user = JsonSerializer.Deserialize<User>(body) ?? new User();
		try
		{
			if (_userService.Login(user) == "Login successful")
			{
				return Results.Ok(_userService.GenerateToken(user.Username, user.Password));
			}
			else
			{
				return Results.Problem("Invalid username or password");
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error logging in: {ex.Message}");
			return Results.Problem("Error logging in");
		}
	}
});

app.MapGet("/api/user/reviews/{token}" , (string token) =>
{
	return _userService.GetUserReviews(token);
});


app.MapPost("/api/user/add", async (HttpRequest request) =>
{
	using (var reader = new StreamReader(request.Body))
	{
		var body = await reader.ReadToEndAsync();
		var user = JsonSerializer.Deserialize<User>(body) ?? new User();
		try{
			return _userService.CreateUser(user);
		}
		catch(Exception ex){
			Console.Error.WriteLine($"Error adding user: {ex.Message}");
			return Results.Problem("Error adding user");
		}
	}
});

app.UseWebSockets();
app.Map("/ws", async context =>
{
	/*if (context.WebSockets.IsWebSocketRequest)
	{
		using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
		{
			while (true)
			{
				Product product = _productService.GenerateFakeProduct();
				_productService.WriteProduct(product);
				var json = JsonSerializer.Serialize(product);
				await webSocket.SendAsync(Encoding.ASCII.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
				await Task.Delay(15000);
			}
		}
	}
	else
	{
		context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
	}
	*/
});


app.Run();
