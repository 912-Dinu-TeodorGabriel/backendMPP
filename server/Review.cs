using System.ComponentModel.DataAnnotations;
using BackEnd;
public class Review
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int ProductId { get; set; }

	[Required]
	public string? Text { get; set; }
	
	[Required]
	public float Rating { get; set; }
	
	[Required]
	public string? Author { get; set; }
}