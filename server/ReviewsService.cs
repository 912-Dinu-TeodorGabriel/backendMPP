using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Net;
using Microsoft.Data.SqlClient;
using BackEnd;
using System.Threading.Tasks;

public class ReviewService
{
	private static string _connectionString = "";

	public ReviewService(string connectionString)
	{
		_connectionString = connectionString;
	}

	public float GetAverageRating (int ProductId)
	{
		//checks if the product has any reviews
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			string query = "SELECT Rating FROM Reviews WHERE ProductId = @ProductId";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@ProductId", string.IsNullOrEmpty(ProductId.ToString()) ? DBNull.Value : (object)ProductId);
				using (SqlDataReader reader = command.ExecuteReader())
				{
					int sum = 0;
					int count = 0;
					while (reader.Read())
					{
						sum += Convert.ToInt32(reader["Rating"]);
						count++;
					}
					return count == 0 ? 0 : (float)sum / count;
				}
			}
		}
	}
	public int GetNumberOfReviewsByProductId(int ProductId)
	{
		//checks if the product has any reviews
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			string query = "SELECT COUNT(*) FROM Reviews WHERE ProductId = @ProductId";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@ProductId", string.IsNullOrEmpty(ProductId.ToString()) ? DBNull.Value : (object)ProductId);
				return (int)command.ExecuteScalar();
			}
		}
	}
	
	public List<Review> GenerateReviews(int count)
	{
		var faker = new Bogus.Faker<Review>()
			.RuleFor(r => r.ProductId, f => f.Random.Number(1, 100)) // Assuming ProductId range is 1 to 100
			.RuleFor(r => r.Text, f => f.Lorem.Paragraph())
			.RuleFor(r => r.Rating, f => f.Random.Float(1, 5))
			.RuleFor(r => r.Author, f => f.Person.FullName);

		return faker.Generate(count);
	}
	
	public async Task GenerateAndInsertReviewsAsync()
	{

		var reviews = GenerateReviews(100000);

		foreach (var review in reviews)
		{
			await AddReviewAsync(review);
		}
	}
	public static async Task AddReviewAsync(Review review)
	{
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			await connection.OpenAsync();

			string query = "INSERT INTO Reviews (ProductId, Text, Rating, Author) VALUES (@ProductId, @Text, @Rating, @Author)";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@ProductId", review.ProductId);
				command.Parameters.AddWithValue("@Text", review.Text);
				command.Parameters.AddWithValue("@Rating", review.Rating);
				command.Parameters.AddWithValue("@Author", review.Author);
				await command.ExecuteNonQueryAsync();
			}
		}
	}
	public async Task<List<Review>> GetReviewsByProductId(int ProductId)
	{
		//read all reviews for a product
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			await connection.OpenAsync();

			string query = "SELECT * FROM Reviews WHERE ProductId = @ProductId";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@ProductId", ProductId);
				using (SqlDataReader reader = await command.ExecuteReaderAsync())
				{
					List<Review> reviews = new List<Review>();
					while (reader.Read())
					{
						reviews.Add(new Review
						{
							Id = Convert.ToInt32(reader["Id"]),
							ProductId = Convert.ToInt32(reader["ProductId"]),
							Text = reader["Text"].ToString(),
							Rating = Convert.ToInt32(reader["Rating"]),
							Author = reader["Author"].ToString()
						});
					}
					return reviews;
				}
			}
		}
	}
	public async Task<bool> DeleteReviewAsync(int reviewId)
	{
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			await connection.OpenAsync();

			string query = "DELETE FROM Reviews WHERE Id = @Id";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@Id", reviewId);
				return await command.ExecuteNonQueryAsync() > 0;
			}
			
		}
	}
	public async Task<bool> UpdateReviewAsync(Review review)
	{
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			await connection.OpenAsync();

			string query = "UPDATE Reviews SET ProductId = @ProductId, Text = @Text, Rating = @Rating WHERE Id = @Id";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@ProductId", review.ProductId);
				command.Parameters.AddWithValue("@Text", review.Text);
				command.Parameters.AddWithValue("@Rating", review.Rating);
				command.Parameters.AddWithValue("@Id", review.Id);
				command.Parameters.AddWithValue("@Author", review.Author);
				return await command.ExecuteNonQueryAsync() > 0;
			}
		}
	}
}
