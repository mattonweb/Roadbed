namespace Roadbed.Test.Integration.Net.Mocks;

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roadbed.Net;
using Roadbed.Crud;

/// <summary>
/// CRUD repository for IntegrationObject entity.
/// </summary>
/// <remarks>
/// The 'live' mock API has a daily limit of requests.
/// The current limit is equal to 100 requests per day. 
/// </remarks>
internal class IntegrationObjectRepository
    : IBaseRepositoryWithCrudl<IntegrationObjectRow, string>
{
    /// <summary>
    /// Base API path for the RESTful API.
    /// </summary>
    private const string BaseApiPath = "https://api.restful-api.dev/objects";

    /// <summary>
    /// Settings to instruct Newtonsoft.Json serializer to ignore null values.
    /// </summary>
    private static readonly JsonSerializerSettings newtonsoftSerializationSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <inheritdoc />
    public async Task<IList<IntegrationObjectRow>> ListAsync(CancellationToken cancellationToken)
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Get,
            HttpEndPoint = new Uri(BaseApiPath),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            List<IntegrationObjectRow>? result =
                JsonConvert.DeserializeObject<List<IntegrationObjectRow>>(response.Data);

            if (result != null)
            {
                return result;
            }
        }

        return default!;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Delete,
            HttpEndPoint = new Uri(string.Join("/", BaseApiPath, id)),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<IntegrationObjectRow> ReadAsync(string id, CancellationToken cancellationToken)
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Get,
            HttpEndPoint = new Uri(string.Join("/", BaseApiPath, id)),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            IntegrationObjectRow? result =
                JsonConvert.DeserializeObject<IntegrationObjectRow>(response.Data);

            if (result != null)
            {
                return result;
            }
        }

        // If we reach here, something went wrong
        return default!;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(IntegrationObjectRow dto, CancellationToken cancellationToken)
    {
        string jsonPayload = JsonConvert.SerializeObject(dto, newtonsoftSerializationSettings);

        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Put,
            HttpEndPoint = new Uri(string.Join("/", BaseApiPath, dto.Id)),
            Content = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json"),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<string> CreateAsync(IntegrationObjectRow dto, CancellationToken cancellationToken)
    {
        string jsonPayload = JsonConvert.SerializeObject(dto, newtonsoftSerializationSettings);

        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Post,
            HttpEndPoint = new Uri(BaseApiPath),
            Content = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json"),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            JObject responseData = JObject.Parse(response.Data);

            string? id = responseData.Value<string>("id");

            return id!;
        }
        else
        {
            return string.Empty;
        }
    }
}
