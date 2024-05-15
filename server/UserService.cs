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
using System.Security.Principal;


public class UserService 
{
	private static string _connectionString = "";
	private const string EncryptionKey = "MPP2024";

	public UserService(string connectionString)
	{
		_connectionString = connectionString;
	}
	

	public string GenerateToken(string username, string password)
	{
		// Concatenate the username and password
		string combinedString = username + ":" + password;

		// Convert the combined string to bytes
		byte[] combinedBytes = Encoding.UTF8.GetBytes(combinedString);

		// Encrypt the combined bytes using XOR encryption
		byte[] encryptedBytes = Encrypt(combinedBytes, EncryptionKey);

		// Convert encrypted bytes to a base64 string
		string encryptedToken = Convert.ToBase64String(encryptedBytes);

		// Return the base64 string as the token
		return encryptedToken;
	}

	public string DecryptToken(string encryptedToken)
	{
		// Convert the base64 string back to encrypted bytes
		byte[] encryptedBytes = Convert.FromBase64String(encryptedToken);

		// Decrypt the encrypted bytes using XOR encryption
		byte[] decryptedBytes = Encrypt(encryptedBytes, EncryptionKey);

		// Convert decrypted bytes to string
		string decryptedString = Encoding.UTF8.GetString(decryptedBytes);

		// Return the decrypted string
		return decryptedString;
	}

	private byte[] Encrypt(byte[] data, string key)
	{
		byte[] keyBytes = Encoding.UTF8.GetBytes(key);
		byte[] encryptedData = new byte[data.Length];

		for (int i = 0; i < data.Length; i++)
		{
			encryptedData[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
		}

		return encryptedData;
	}
	
	public IResult CreateUser(User user)
	{
		//check if the userName already exists
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			string query = "SELECT * FROM Users WHERE Username = @Username";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@Username", string.IsNullOrEmpty(user.Username) ? DBNull.Value : (object)user.Username);
				using (SqlDataReader reader = command.ExecuteReader())
				{
					if (reader.Read())
					{
						return Results.Problem("Username already exists");
					}
				}
			}
		}
		
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			string query = "INSERT INTO Users (Username, Password, Email) VALUES (@Username, @Password, @Email); SELECT SCOPE_IDENTITY()";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@Username", string.IsNullOrEmpty(user.Username) ? DBNull.Value : (object)user.Username);
				command.Parameters.AddWithValue("@Password", string.IsNullOrEmpty(user.Password) ? DBNull.Value : (object)user.Password);
				command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(user.Email) ? DBNull.Value : (object)user.Email);
				user.Id = Convert.ToInt32(command.ExecuteScalar());
				return Results.Ok("User added successfully");
			}
		}
		
	}
	
	public string Login(User user)
	{
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			string query = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@Username", string.IsNullOrEmpty(user.Username) ? DBNull.Value : (object)user.Username);
				command.Parameters.AddWithValue("@Password", string.IsNullOrEmpty(user.Password) ? DBNull.Value : (object)user.Password);
				using (SqlDataReader reader = command.ExecuteReader())
				{
					if (reader.Read())
					{
						return "Login successful";
					}
					else
					{
						return "Invalid username or password";
					}
				}
			}
		}
	}
	//getting all reviews of an user
	public List<Review> GetUserReviews(string token)
	{
		string userName = DecryptToken(token).Split(":")[0];
		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			string query = "SELECT * FROM Reviews WHERE Author = @userName";
			using (SqlCommand command = new SqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@userName", userName);
				using (SqlDataReader reader = command.ExecuteReader())
				{
					List<Review> reviews = new List<Review>();
					while (reader.Read())
					{
						Review review = new Review();
						review.Id = reader.GetInt32(0);
						review.ProductId = reader.GetInt32(1);
						review.Text = reader.GetString(2);
						review.Rating = float.Parse(reader.GetDouble(3).ToString());
						review.Author = reader.GetString(4);
						reviews.Add(review);
					}
					return reviews;
				}
			}
		}
	}
}