using Microsoft.EntityFrameworkCore;
using StreamingProgress.Data;

var builder = WebApplication.CreateBuilder(args);

const string ConnectionString =
    "Server=127.0.0.1,1433;Database=StreamingDb;User Id=sa;Password=Your_strong_Pass123;TrustServerCertificate=True;Encrypt=False;";

builder.Services.AddDbContext<StreamingDbContext>(options =>
    options.UseSqlServer(ConnectionString));

builder.Services.AddScoped<StreamingProgress.Services.IPlaybackProgressService,
    StreamingProgress.Services.PlaybackProgressService>();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StreamingDbContext>();
    await DbInitializer.InitializeAsync(db, CancellationToken.None);
}

app.MapControllers();

app.Run();

public partial class Program { }

