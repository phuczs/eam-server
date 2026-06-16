using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EAM.Api.Custom;

/// <summary>
/// JSON result prefixed with an anti-JSON-hijacking guard (")]}',\n"). The SPA must strip
/// the prefix before parsing. Use for sensitive list endpoints where defence-in-depth helps.
/// </summary>
public class SecureJsonResult : IActionResult
{
    private const string AntiHijackPrefix = ")]}',\n";
    private readonly object _value;

    public SecureJsonResult(object value) => _value = value;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json; charset=utf-8";

        await response.WriteAsync(AntiHijackPrefix);

        var json = JsonSerializer.Serialize(_value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await response.WriteAsync(json);
    }
}
