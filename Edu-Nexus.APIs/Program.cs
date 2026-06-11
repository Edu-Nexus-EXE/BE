using Edu_Nexus.APIs.Extensions;
using Edu_Nexus.Application;
using Edu_Nexus.Infrastructure;
using Edu_Nexus.Infrastructure.Data;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyFrontend",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5175",
                    "https://edu-nexus-web.vercel.app",
                    "http://localhost:5173"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Swagger bật khi: môi trường Development, HOẶC bật cờ Swagger:Enabled=true qua env/config.
// → Trên production có thể mở Swagger bằng cách set env `Swagger__Enabled=true` (KHÔNG cần sửa code,
//   KHÔNG cần đổi ASPNETCORE_ENVIRONMENT — tránh side-effect của việc lật cả môi trường).
var enableSwagger = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Edu-Nexus API v1");
        options.RoutePrefix = string.Empty;
        options.InjectStylesheet("/swagger-ui/custom.css");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowMyFrontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync(CancellationToken.None);

    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    
    recurringJobManager.AddOrUpdate<Edu_Nexus.Infrastructure.Jobs.SubscriptionExpirationJob>(
        "subscription-expiration-job",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<Edu_Nexus.Infrastructure.Jobs.RenewalNotificationJob>(
        "subscription-renewal-notification-job",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<Edu_Nexus.Infrastructure.Jobs.ExpirePendingPaymentOrdersJob>(
        "expire-pending-payment-orders",
        job => job.RunAsync(CancellationToken.None),
        "*/5 * * * *");
}

app.MapControllers();

app.Run();

public partial class Program;
