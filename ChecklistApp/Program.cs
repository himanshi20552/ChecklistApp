using ChecklistApp.Data;
using ChecklistApp.Services;
namespace ChecklistApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
			builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
			builder.Services.AddScoped<IChecklistService, ChecklistService>();
			builder.Services.AddScoped<IRequirementService, RequirementService>();
			builder.Services.AddScoped<IDemoPolicyService, DemoPolicyService>();
			builder.Services.AddControllers();
			builder.Services.AddOpenApi();

			var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
			if (app.Environment.IsDevelopment())
			{
				app.MapOpenApi();

				app.UseSwaggerUI(options =>
				{
					options.SwaggerEndpoint(
						"/openapi/v1.json",
						"Checklist API");
				});
			}
			app.UseHttpsRedirection();

            app.UseRouting();
			app.MapControllers();
			app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
