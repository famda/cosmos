using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "AccountEndpoint=https://cosmos-db:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
var databaseName = "cosmos-database";

builder.Services.AddDbContext<CosmosDbContext>(options =>
    options.UseCosmos(connectionString, databaseName, cosmosOptions => {
        cosmosOptions.HttpClientFactory(() => {
            var handler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Bypass SSL validation
            };
            return new HttpClient(handler);
        });
        cosmosOptions.ConnectionMode(ConnectionMode.Gateway);
    }));

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {

    var dbContext = scope.ServiceProvider.GetRequiredService<CosmosDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

app.MapGet("/test", async (CosmosDbContext context, CancellationToken cancellationToken) => {
    var users = await context.Files.ToListAsync(cancellationToken);
    return users;
});

app.Run();

public class CosmosDbContext(DbContextOptions<CosmosDbContext> options) : DbContext(options) {
    protected override void OnModelCreating(ModelBuilder builder) {

        builder.HasDefaultContainer("Files");

        builder.Entity<FileItem>(eb => { 
            eb.ToContainer("Files");
            eb.HasKey(u => u.Id);
            eb.Property(x => x.Id).ToJsonProperty("id");
            eb.Property(x => x.Name).ToJsonProperty("name");
            eb.Property(b => b.Id).HasValueGenerator<GuidValueGenerator>();
            eb.HasPartitionKey(u => u.Id);
            eb.HasNoDiscriminator();
        });
    }
    public DbSet<FileItem> Files { get; set; }
}

public class FileItem {
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}



