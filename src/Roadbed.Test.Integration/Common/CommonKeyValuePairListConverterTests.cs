namespace Roadbed.Test.Integration;

using Newtonsoft.Json;

/// <summary>
/// Contains unit tests for verifying the behavior of the NetService class.
/// </summary>
[TestClass]
public class CommonKeyValuePairListConverterTests
{
    /*
    #region Public Methods

    /// <summary>
    /// Unit test to verify Newtonsoft deserialization.
    /// </summary>
    [TestMethod]
    public void CommonKeyValuePairListConverter_Deserialize_StaticExpectedJson()
    {
        string json = @"{""id"":""ff8081819782e69e019b48833dcd5c23"",""name"":""Test Object"",""data"":{""Color"":""Red"",""Year"":""2024""}}";

        IntegrationObjectRow? obj = JsonConvert.DeserializeObject<IntegrationObjectRow>(json);

        // Access the data
        if (obj is not null)
        {
            Console.WriteLine($"ID: {obj.Id}");
            Console.WriteLine($"Name: {obj.Name}");
            Console.WriteLine($"Attributes Count: {obj.Attributes.Count}");

            foreach (var attr in obj.Attributes)
            {
                Console.WriteLine($"  {attr.Key}: {attr.Value}");
            }
        }

        Assert.IsNotNull(
            obj,
            "The returned row is null.");
        Assert.HasCount(
            2,
            obj.Attributes,
            $"There should be 2 rows in the collection.");
    }

    #endregion Public Methods

    */
}