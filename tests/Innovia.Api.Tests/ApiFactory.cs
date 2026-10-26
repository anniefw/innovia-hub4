using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Innovia.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString) => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("OpenAI:ApiKey", "test-key-not-used");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            });
        });
    }

    public HttpClient CreateAuthClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
        // Cookie is marked Secure; CookieContainer only attaches Secure cookies to
        // https:// requests, even against the in-memory TestServer transport.
        BaseAddress = new Uri("https://localhost")
    });
}
