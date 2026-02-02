namespace Roadbed.Test.Integration;

using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed.Net;
using Roadbed.Net.Installers;

/// <summary>
/// Contains unit tests for verifying the behavior of the NetService class.
/// </summary>
[TestClass]
public class SimpleNetServiceTests
{
    /*
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleNetServiceTests"/> class.
    /// </summary>
    public SimpleNetServiceTests()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var installer = new InstallNetHttpClient();

        // Install HTTP Client Factory
        installer.ConfigureServices(services, configuration);
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext TestContext { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Unit test to verify that BaseEntityWithCrudl creates a row with the ID correctly.
    /// </summary>
    /// <returns>A unit of work representing when operation has been completed.</returns>
    [TestMethod]
    public async Task NetService_HttpClient_RowsReturned()
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Get,
            HttpEndPoint = new Uri("https://api.weather.gov/gridpoints/OAX/83,60/forecast"),
            HttpHeaders = new List<NetHttpHeader>()
            {
                { new NetHttpHeader("User-Agent", "Intergration Test") },
                { new NetHttpHeader("Accept", "application/geo+json") },
            },
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, this.TestContext.CancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Http Request wasn't successful.");
    }

    #endregion Public Methods

    */
}