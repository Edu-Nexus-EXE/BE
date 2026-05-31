using Edu_Nexus.APIs.Extensions;
using Edu_Nexus.Application;
using Edu_Nexus.Infrastructure;
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
            policy.WithOrigins("http://localhost:5173") // Điền địa chỉ frontend của bạn vào đây
                  .AllowAnyHeader()
                  .AllowAnyMethod();
                  // .AllowCredentials(); // (Mở comment dòng này nếu bạn dùng cookie/session)
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
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

app.MapControllers();

app.Run();
