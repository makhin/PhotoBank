using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PhotoBank.UnitTests.Infrastructure.Http;

internal sealed class HttpResponseBuilder
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string? _content;
    private string? _mediaType;

    public static HttpResponseBuilder Create() => new();

    public HttpResponseBuilder WithStatus(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    public HttpResponseBuilder WithJson(object payload)
    {
        _content = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _mediaType = "application/json";
        return this;
    }

    public HttpResponseBuilder WithJson(string rawJson)
    {
        _content = rawJson;
        _mediaType = "application/json";
        return this;
    }

    public HttpResponseBuilder WithText(string text, string mediaType = "text/plain")
    {
        _content = text;
        _mediaType = mediaType;
        return this;
    }

    public HttpResponseBuilder WithoutContent()
    {
        _content = null;
        _mediaType = null;
        return this;
    }

    public HttpResponseBuilder WithError(string code, string message)
    {
        return WithJson(new { error = new { code, message } });
    }

    public HttpResponseMessage Build()
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_content != null)
        {
            response.Content = new StringContent(_content, Encoding.UTF8, _mediaType ?? "text/plain");
        }

        return response;
    }
}
