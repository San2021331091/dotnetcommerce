using Npgsql;
using DotNetEnv;

Env.Load(); // Load .env variables

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Your server is running");

// Build connection string from environment variables
string connectionString = $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
                          $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                          $"Password={Environment.GetEnvironmentVariable("DB_PASS")};" +
                          $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                          $"SslMode={Environment.GetEnvironmentVariable("DB_SSLMODE")};" +
                          $"Trust Server Certificate={Environment.GetEnvironmentVariable("DB_TRUST_CERT")};";

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
            reader.GetInt32(0),                // id
            reader.GetString(1),               // user_uid
            reader.GetInt32(2),                // product_id
            reader.GetString(3),               // img_url
            reader.GetInt32(4),                // quantity
            reader.GetDecimal(5),              // price
            reader.GetFieldValue<DateTimeOffset>(6), // ordered_at (timestamp with time zone)
            reader.IsDBNull(7) ? "pending" : reader.GetString(7)  // status (handle possible NULL)
        ));
    }

    return Results.Ok(orders);
});

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
            reader.GetInt32(0),                // id
            reader.GetString(1),               // user_uid
            reader.GetInt32(2),                // product_id
            reader.GetString(3),               // img_url
            reader.GetInt32(4),                // quantity
            reader.GetDecimal(5),              // price
            reader.GetFieldValue<DateTimeOffset>(6), // ordered_at
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
