using System.Collections.Generic;

namespace PhotoBank.IntegrationTests.Infra;

public static class TestConfiguration
{
    public static Dictionary<string, string?> Build(
        string connectionString,
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var configuration = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience",
            ["Jwt:Key"] = "super-secret"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                configuration[key] = value;
            }
        }

        return configuration;
    }
}
