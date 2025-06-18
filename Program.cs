using Npgsql;
using DotNetEnv;

Env.Load(); // Load .env variables

var builder = WebApplication.CreateBuilder(args);

// ✅ Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ Enable CORS middleware
app.UseCors("AllowAll");

app.MapGet("/", () => "Your server is running");

// ✅ Connection string
string connectionString = $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
                          $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                          $"Password={Environment.GetEnvironmentVariable("DB_PASS")};" +
                          $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                          $"SslMode={Environment.GetEnvironmentVariable("DB_SSLMODE")};" +
                          $"Trust Server Certificate={Environment.GetEnvironmentVariable("DB_TRUST_CERT")};";

// ✅ GET: All orders
app.MapGet("/orders", async () =>
{
    var orders = new List<Order>();

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    const string query = @"
        SELECT 
            id, user_uid, product_id, img_url, quantity, price, ordered_at, status
        FROM orders;";

    await using var cmd = new NpgsqlCommand(query, conn);
    await using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        orders.Add(new Order(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetDecimal(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.IsDBNull(7) ? "pending" : reader.GetString(7)
        ));
    }

    return Results.Ok(orders);
});

// ✅ GET: Orders by userUid
app.MapGet("/orders/{userUid}", async (string userUid) =>
{
    var orders = new List<Order>();

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    const string query = @"
        SELECT 
            id, user_uid, product_id, img_url, quantity, price, ordered_at, status
        FROM orders
        WHERE user_uid = @userUid;";

    await using var cmd = new NpgsqlCommand(query, conn);
    cmd.Parameters.AddWithValue("userUid", userUid);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        orders.Add(new Order(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetDecimal(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.IsDBNull(7) ? "pending" : reader.GetString(7)
        ));
    }

    return Results.Ok(orders);
});

app.Run();

record Order(
    int Id,
    string UserUid,
    int ProductId,
    string ImgUrl,
    int Quantity,
    decimal Price,
    DateTimeOffset OrderedAt,
    string Status
);
