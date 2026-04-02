using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Hat.Models;
using Hat.Models.NetworkModels;

namespace Hat.Services;

/// <summary>
/// Fetches available models from provider APIs.
/// Port of ModelFetcher.swift.
/// </summary>
public class ModelFetcher
{
    private static readonly Lazy<ModelFetcher> _instance = new(() => new ModelFetcher());
    public static ModelFetcher Shared => _instance.Value;

    private readonly HttpClient _httpClient;

    private ModelFetcher()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    /// <summary>
    /// Fetches the list of available model IDs for a given provider and API key.
    /// </summary>
    public async Task<List<string>> FetchModelsAsync(CloudProvider provider, string apiKey, CancellationToken ct = default)
    {
        var endpointString = provider.ModelsEndpoint();
        if (string.IsNullOrEmpty(endpointString))
            return new List<string>();

        var request = new HttpRequestMessage(HttpMethod.Get, endpointString);

        if (!string.IsNullOrEmpty(apiKey))
        {
            switch (provider)
            {
                case CloudProvider.Anthropic:
                    request.Headers.Add("x-api-key", apiKey);
                    request.Headers.Add("anthropic-version", "2023-06-01");
                    break;
                default:
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    break;
            }
        }

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var body = await response.Content.ReadAsStringAsync(ct);
        var decoded = JsonSerializer.Deserialize<APIModelListResponse>(body);

        return decoded?.Data?
            .Select(m => m.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .OrderBy(id => id)
            .ToList() ?? new List<string>();
    }
}
