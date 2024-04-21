using CosmosDb.WebApp.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CosmosDb.WebApp.WebApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddOptions();
			builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAnyCorsPolicy", builder => {
					builder
						.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader();
				});
			}); 
			
			var app = builder.Build();

			app.UseSwagger();
			app.UseSwaggerUI();
			app.UseCors("AllowAnyCorsPolicy");
			app.UseHttpsRedirection();
			app.MapControllers();
			app.Run();
		}
	}
}
