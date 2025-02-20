var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddSingleton<RedisService>();

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

Console.WriteLine("Hello, World!");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
