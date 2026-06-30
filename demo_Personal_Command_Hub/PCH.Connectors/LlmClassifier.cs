using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PCH.Core.Models;

namespace PCH.Connectors;

/// <summary>
/// Classifies emails by calling a local GPT4All instance (OpenAI-compatible API at Llm:BaseUrl).
/// Produces an <see cref="EmailType"/> category and a 1–2 sentence summary.
/// </summary>
public class LlmClassifier
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<LlmClassifier> _logger;

    private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Initialises the classifier with a pre-configured HttpClient (base address = Llm:BaseUrl).</summary>
    public LlmClassifier(HttpClient http, IConfiguration config, ILogger<LlmClassifier> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends the email to the local LLM for classification and summarisation.
    /// Returns <c>(EmailType.Other, null)</c> on any LLM or parse failure so the caller can continue.
    /// </summary>
    /// <param name="subject">Email subject line.</param>
    /// <param name="sender">Sender display name / address.</param>
    /// <param name="bodyPreview">First ~500 chars of the email body.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<(EmailType Type, string? Summary)> ClassifyAsync(
        string subject, string sender, string bodyPreview, CancellationToken ct = default)
    {
        var model = _config["Llm:Model"] ?? "Llama 3.2 1B Instruct";

        var userContent = $$"""
            Subject: {{subject}}
            From: {{sender}}
            Body: {{bodyPreview}}

            Respond with this exact JSON (no other text):
            {"type":"booking|deadline|meeting|invoice|other","summary":"1-2 sentence summary"}
            """;

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = "You are an email classifier. Analyze the email and respond with JSON only. The type field must be exactly one of: booking, deadline, meeting, invoice, other." },
                new { role = "user",   content = userContent }
            },
            temperature = 0.1,
            max_tokens = 200
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/v1/chat/completions", requestBody, ct);
            response.EnsureSuccessStatusCode();

            using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var raw = doc?.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var json = StripCodeFences(raw.Trim());

            using var result = JsonDocument.Parse(json);
            var typeStr  = result.RootElement.GetProperty("type").GetString() ?? "other";
            var summary  = result.RootElement.GetProperty("summary").GetString();

            var emailType = typeStr.Trim().ToLowerInvariant() switch
            {
                "booking"  => EmailType.Booking,
                "deadline" => EmailType.Deadline,
                "meeting"  => EmailType.Meeting,
                "invoice"  => EmailType.Invoice,
                _          => EmailType.Other
            };

            return (emailType, summary);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM classification failed for subject '{Subject}'", subject);
            return (EmailType.Other, null);
        }
    }

    private static string StripCodeFences(string s)
    {
        if (!s.StartsWith("```", StringComparison.Ordinal))
            return s;
        var lines = s.Split('\n');
        // Drop the opening ```json / ``` line and the closing ``` line
        return string.Join('\n', lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```", StringComparison.Ordinal)));
    }
}
