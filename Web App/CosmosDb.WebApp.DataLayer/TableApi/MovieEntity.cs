using Azure;
using Azure.Data.Tables;
using System;

namespace CosmosDb.WebApp.DataLayer.TableApi
{
	public class MovieEntity : ITableEntity
	{
		public string Genre { get; set; }
		public string Title { get; set; }
		public int Year { get; set; }
		public string Length { get; set; }
		public string Description { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }

		public string PartitionKey
		{
			get => this.Genre;
			set => this.Genre = value;
		}

		public string RowKey
		{
			get => this.Title;
			set => this.Title = value;
		}

		public MovieEntity(string genre, string title)
		{
			this.Genre = genre;
			this.Title = title;
		}

	}

}
