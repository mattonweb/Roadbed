namespace Roadbed.Test.Unit.Common;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Common;
using Roadbed.Common.Converters;

/// <summary>
/// Unit tests for the <see cref="CommonKeyValuePairListConverter{TKey, TValue}"/> class.
/// </summary>
[TestClass]
public class CommonKeyValuePairListConverterTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the converter works with the shared <see cref="RoadbedJson.Options"/>.
    /// </summary>
    [TestMethod]
    public void Integration_WithRoadbedJsonOptions_ShouldWorkCorrectly()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Key", "Value"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        var deserialized = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Data);
        Assert.HasCount(1, deserialized.Data);
        Assert.AreEqual("Key", deserialized.Data[0].Key);
        Assert.AreEqual("Value", deserialized.Data[0].Value);
    }

    /// <summary>
    /// Verifies that Read works with boolean values.
    /// </summary>
    [TestMethod]
    public void Read_WithBooleanValues_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""IsActive"":true,""IsDeleted"":false}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObjectWithBoolValue>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("IsActive", result.Data[0].Key);
        Assert.IsTrue(result.Data[0].Value);
        Assert.AreEqual("IsDeleted", result.Data[1].Key);
        Assert.IsFalse(result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read works with decimal values.
    /// </summary>
    [TestMethod]
    public void Read_WithDecimalValues_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""Price"":19.99,""Tax"":1.50}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObjectWithDecimalValue>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("Price", result.Data[0].Key);
        Assert.AreEqual(19.99m, result.Data[0].Value);
        Assert.AreEqual("Tax", result.Data[1].Key);
        Assert.AreEqual(1.50m, result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read handles empty JSON object correctly.
    /// </summary>
    [TestMethod]
    public void Read_WithEmptyObject_ShouldReturnEmptyList()
    {
        // Arrange
        string json = @"{""data"":{}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.IsEmpty(result.Data);
    }

    /// <summary>
    /// Verifies that Read works with integer keys.
    /// </summary>
    [TestMethod]
    public void Read_WithIntegerKeys_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""1"":""First"",""2"":""Second""}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObjectWithIntKey>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual(1, result.Data[0].Key);
        Assert.AreEqual("First", result.Data[0].Value);
        Assert.AreEqual(2, result.Data[1].Key);
        Assert.AreEqual("Second", result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read works with integer values.
    /// </summary>
    [TestMethod]
    public void Read_WithIntegerValues_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""Age"":30,""Count"":100}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObjectWithIntValue>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("Age", result.Data[0].Key);
        Assert.AreEqual(30, result.Data[0].Value);
        Assert.AreEqual("Count", result.Data[1].Key);
        Assert.AreEqual(100, result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read handles null data correctly.
    /// </summary>
    [TestMethod]
    public void Read_WithNullData_ShouldReturnNull()
    {
        // Arrange
        string json = @"{""data"":null}";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.Data);
    }

    /// <summary>
    /// Verifies that Read handles null values correctly.
    /// </summary>
    [TestMethod]
    public void Read_WithNullValue_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""Key1"":""Value1"",""Key2"":null}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("Key1", result.Data[0].Key);
        Assert.AreEqual("Value1", result.Data[0].Value);
        Assert.AreEqual("Key2", result.Data[1].Key);
        Assert.IsNull(result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read handles special characters in keys.
    /// </summary>
    [TestMethod]
    public void Read_WithSpecialCharactersInKeys_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""Key-With-Dashes"":""Value1"",""Key_With_Underscores"":""Value2""}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("Key-With-Dashes", result.Data[0].Key);
        Assert.AreEqual("Value1", result.Data[0].Value);
        Assert.AreEqual("Key_With_Underscores", result.Data[1].Key);
        Assert.AreEqual("Value2", result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read handles Unicode characters correctly.
    /// </summary>
    [TestMethod]
    public void Read_WithUnicodeCharacters_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""名前"":""太郎"",""città"":""Roma""}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("名前", result.Data[0].Key);
        Assert.AreEqual("太郎", result.Data[0].Value);
        Assert.AreEqual("città", result.Data[1].Key);
        Assert.AreEqual("Roma", result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Read correctly converts JSON object to list of pairs.
    /// </summary>
    [TestMethod]
    public void Read_WithValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = @"{""data"":{""Color"":""Red"",""Year"":""2024""}}";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.AreEqual("Color", result.Data[0].Key);
        Assert.AreEqual("Red", result.Data[0].Value);
        Assert.AreEqual("Year", result.Data[1].Key);
        Assert.AreEqual("2024", result.Data[1].Value);
    }

    /// <summary>
    /// Verifies that serialization followed by deserialization produces equivalent data.
    /// </summary>
    [TestMethod]
    public void RoundTrip_SerializeAndDeserialize_ShouldPreserveData()
    {
        // Arrange
        var original = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Color", "Blue"),
                new CommonKeyValuePair<string, string>("Size", "Large"),
                new CommonKeyValuePair<string, string>("Year", "2025"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(original, RoadbedJson.Options);
        var deserialized = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Data);
        Assert.HasCount(original.Data.Count, deserialized.Data);

        for (int i = 0; i < original.Data.Count; i++)
        {
            Assert.AreEqual(original.Data[i].Key, deserialized.Data[i].Key);
            Assert.AreEqual(original.Data[i].Value, deserialized.Data[i].Value);
        }
    }

    /// <summary>
    /// Verifies round-trip with integer types.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WithIntegerTypes_ShouldPreserveData()
    {
        // Arrange
        var original = new TestObjectWithIntValue
        {
            Data = new List<CommonKeyValuePair<string, int>>
            {
                new CommonKeyValuePair<string, int>("Count", 42),
                new CommonKeyValuePair<string, int>("Age", 25),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(original, RoadbedJson.Options);
        var deserialized = JsonSerializer.Deserialize<TestObjectWithIntValue>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Data);
        Assert.HasCount(original.Data.Count, deserialized.Data);

        for (int i = 0; i < original.Data.Count; i++)
        {
            Assert.AreEqual(original.Data[i].Key, deserialized.Data[i].Key);
            Assert.AreEqual(original.Data[i].Value, deserialized.Data[i].Value);
        }
    }

    /// <summary>
    /// Verifies round-trip with null values.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WithNullValues_ShouldPreserveNulls()
    {
        // Arrange
        var original = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Key1", "Value1"),
                new CommonKeyValuePair<string, string>("Key2", null!),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(original, RoadbedJson.Options);
        var deserialized = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Data);
        Assert.HasCount(original.Data.Count, deserialized.Data);
        Assert.AreEqual("Key1", deserialized.Data[0].Key);
        Assert.AreEqual("Value1", deserialized.Data[0].Value);
        Assert.AreEqual("Key2", deserialized.Data[1].Key);
        Assert.IsNull(deserialized.Data[1].Value);
    }

    /// <summary>
    /// Verifies that Write works with boolean values.
    /// </summary>
    [TestMethod]
    public void Write_WithBooleanValues_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new TestObjectWithBoolValue
        {
            Data = new List<CommonKeyValuePair<string, bool>>
            {
                new CommonKeyValuePair<string, bool>("IsActive", true),
                new CommonKeyValuePair<string, bool>("IsDeleted", false),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.IsTrue(data["IsActive"]!.GetValue<bool>());
        Assert.IsFalse(data["IsDeleted"]!.GetValue<bool>());
    }

    /// <summary>
    /// Verifies that Write works with decimal values.
    /// </summary>
    [TestMethod]
    public void Write_WithDecimalValues_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new TestObjectWithDecimalValue
        {
            Data = new List<CommonKeyValuePair<string, decimal>>
            {
                new CommonKeyValuePair<string, decimal>("Price", 19.99m),
                new CommonKeyValuePair<string, decimal>("Tax", 1.50m),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual(19.99m, data["Price"]!.GetValue<decimal>());
        Assert.AreEqual(1.50m, data["Tax"]!.GetValue<decimal>());
    }

    /// <summary>
    /// Verifies that Write handles duplicate keys by keeping the last occurrence.
    /// </summary>
    [TestMethod]
    public void Write_WithDuplicateKeys_ShouldSerializeAllOccurrences()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Key", "FirstValue"),
                new CommonKeyValuePair<string, string>("Key", "SecondValue"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);

        // JsonObject rejects duplicate keys at parse time, so deserialize the
        // inner object into a Dictionary, which applies the canonical
        // last-wins resolution for duplicate JSON property names.
        using JsonDocument doc = JsonDocument.Parse(json);
        string innerJson = doc.RootElement.GetProperty("data").GetRawText();
        Dictionary<string, string>? data = JsonSerializer.Deserialize<Dictionary<string, string>>(
            innerJson,
            RoadbedJson.Options);

        // Assert
        // The raw JSON must contain both writes, evidencing that the converter
        // did not deduplicate; JSON parsers MUST resolve dup keys last-wins.
        Assert.Contains("\"FirstValue\"", json);
        Assert.Contains("\"SecondValue\"", json);
        Assert.IsNotNull(data);
        Assert.AreEqual("SecondValue", data["Key"]);
    }

    /// <summary>
    /// Verifies that Write handles empty list correctly.
    /// </summary>
    [TestMethod]
    public void Write_WithEmptyList_ShouldSerializeAsEmptyObject()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>(),
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject root = parsed!.AsObject();
        JsonObject data = root["data"]!.AsObject();

        // Assert
        Assert.IsNotNull(data);
        Assert.AreEqual(0, data.Count);
    }

    /// <summary>
    /// Verifies that Write works with Guid keys.
    /// </summary>
    [TestMethod]
    public void Write_WithGuidKeys_ShouldSerializeCorrectly()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var obj = new TestObjectWithGuidKey
        {
            Data = new List<CommonKeyValuePair<Guid, string>>
            {
                new CommonKeyValuePair<Guid, string>(guid1, "Value1"),
                new CommonKeyValuePair<Guid, string>(guid2, "Value2"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("Value1", data[guid1.ToString()]!.GetValue<string>());
        Assert.AreEqual("Value2", data[guid2.ToString()]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that Write works with integer keys.
    /// </summary>
    [TestMethod]
    public void Write_WithIntegerKeys_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new TestObjectWithIntKey
        {
            Data = new List<CommonKeyValuePair<int, string>>
            {
                new CommonKeyValuePair<int, string>(1, "First"),
                new CommonKeyValuePair<int, string>(2, "Second"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("First", data["1"]!.GetValue<string>());
        Assert.AreEqual("Second", data["2"]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that Write works with integer values.
    /// </summary>
    [TestMethod]
    public void Write_WithIntegerValues_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new TestObjectWithIntValue
        {
            Data = new List<CommonKeyValuePair<string, int>>
            {
                new CommonKeyValuePair<string, int>("Age", 30),
                new CommonKeyValuePair<string, int>("Count", 100),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual(30, data["Age"]!.GetValue<int>());
        Assert.AreEqual(100, data["Count"]!.GetValue<int>());
    }

    /// <summary>
    /// Verifies that Write handles keys with ToString returning empty string.
    /// </summary>
    [TestMethod]
    public void Write_WithKeyToStringReturningEmpty_ShouldSerializeWithEmptyKey()
    {
        // Arrange
        var obj = new TestObjectWithCustomKey
        {
            Data = new List<CommonKeyValuePair<CustomKeyWithEmptyToString, string>>
            {
                new CommonKeyValuePair<CustomKeyWithEmptyToString, string>(new CustomKeyWithEmptyToString(), "Value1"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("Value1", data[string.Empty]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that Write handles keys with ToString returning null.
    /// </summary>
    [TestMethod]
    public void Write_WithKeyToStringReturningNull_ShouldSerializeWithEmptyKey()
    {
        // Arrange
        var obj = new TestObjectWithCustomKeyNull
        {
            Data = new List<CommonKeyValuePair<CustomKeyWithNullToString, string>>
            {
                new CommonKeyValuePair<CustomKeyWithNullToString, string>(new CustomKeyWithNullToString(), "Value1"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("Value1", data[string.Empty]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that Write skips pairs with null keys.
    /// </summary>
    [TestMethod]
    public void Write_WithNullKey_ShouldSkipPair()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("ValidKey", "ValidValue"),
                new CommonKeyValuePair<string, string>(null!, "ValueWithNullKey"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("ValidValue", data["ValidKey"]!.GetValue<string>());
        Assert.AreEqual(1, data.Count);
    }

    /// <summary>
    /// Verifies that Write handles null list correctly.
    /// </summary>
    [TestMethod]
    public void Write_WithNullList_ShouldOmitProperty()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = null,
        };

        // Act — the shared options use DefaultIgnoreCondition.WhenWritingNull,
        // so a null Data property is omitted from the output entirely (mirroring
        // Newtonsoft's NullValueHandling.Ignore).
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject root = parsed!.AsObject();

        // Assert
        Assert.IsFalse(
            root.ContainsKey("data"),
            "Null Data should be omitted under WhenWritingNull (Newtonsoft NullValueHandling.Ignore parity).");
    }

    /// <summary>
    /// Verifies that Write handles pairs with null values correctly.
    /// </summary>
    [TestMethod]
    public void Write_WithNullValue_ShouldSerializeNullValue()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Key1", "Value1"),
                new CommonKeyValuePair<string, string>("Key2", null!),
            },
        };

        // Act — the converter writes pair values directly through the property
        // name path, bypassing DefaultIgnoreCondition. A null pair value still
        // appears in output as a literal null.
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("Value1", data["Key1"]!.GetValue<string>());
        Assert.IsNull(data["Key2"]);
    }

    /// <summary>
    /// Verifies that Write produces the correct JSON structure with string key-value pairs.
    /// </summary>
    [TestMethod]
    public void Write_WithStringPairs_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Color", "Red"),
                new CommonKeyValuePair<string, string>("Year", "2024"),
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        JsonNode? parsed = JsonNode.Parse(json);
        JsonObject data = parsed!["data"]!.AsObject();

        // Assert
        Assert.AreEqual("Red", data["Color"]!.GetValue<string>());
        Assert.AreEqual("2024", data["Year"]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that Write correctly serializes when pair Value property is explicitly null.
    /// </summary>
    [TestMethod]
    public void Write_WithPairValueNull_ShouldSerializeNullCorrectly()
    {
        // Arrange
        var obj = new TestObject
        {
            Data = new List<CommonKeyValuePair<string, string>>
            {
                new CommonKeyValuePair<string, string>("Key1", "Value1"),
                new CommonKeyValuePair<string, string>("Key2", null!),
                new CommonKeyValuePair<string, string>("Key3", "Value3"),
                null!,
            },
        };

        // Act
        string json = JsonSerializer.Serialize(obj, RoadbedJson.Options);
        var deserialized = JsonSerializer.Deserialize<TestObject>(json, RoadbedJson.Options);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Data);
        Assert.HasCount(3, deserialized.Data);
        Assert.AreEqual("Key1", deserialized.Data[0].Key);
        Assert.AreEqual("Value1", deserialized.Data[0].Value);
        Assert.AreEqual("Key2", deserialized.Data[1].Key);
        Assert.IsNull(deserialized.Data[1].Value);
        Assert.AreEqual("Key3", deserialized.Data[2].Key);
        Assert.AreEqual("Value3", deserialized.Data[2].Value);

        // Verify the JSON contains the null value
        Assert.Contains("\"Key2\":null", json);
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Custom key class that returns empty string from ToString.
    /// </summary>
    private class CustomKeyWithEmptyToString
    {
        #region Public Methods

        /// <summary>
        /// Returns an empty string.
        /// </summary>
        /// <returns>Empty string.</returns>
        public override string ToString() => string.Empty;

        #endregion Public Methods
    }

    /// <summary>
    /// Custom key class that returns null from ToString.
    /// </summary>
    private class CustomKeyWithNullToString
    {
        #region Public Methods

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <returns>Null value.</returns>
        public override string? ToString() => null;

        #endregion Public Methods
    }

    /// <summary>
    /// Test class that uses the converter for serialization/deserialization.
    /// </summary>
    private class TestObject
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with string key and string value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<string, string>))]
        public IList<CommonKeyValuePair<string, string>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with boolean values.
    /// </summary>
    private class TestObjectWithBoolValue
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with string key and boolean value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<string, bool>))]
        public IList<CommonKeyValuePair<string, bool>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with custom key type returning empty ToString.
    /// </summary>
    private class TestObjectWithCustomKey
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with custom key and string value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<CustomKeyWithEmptyToString, string>))]
        public IList<CommonKeyValuePair<CustomKeyWithEmptyToString, string>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with custom key type returning null ToString.
    /// </summary>
    private class TestObjectWithCustomKeyNull
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with custom key and string value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<CustomKeyWithNullToString, string>))]
        public IList<CommonKeyValuePair<CustomKeyWithNullToString, string>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with decimal values.
    /// </summary>
    private class TestObjectWithDecimalValue
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with string key and decimal value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<string, decimal>))]
        public IList<CommonKeyValuePair<string, decimal>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with Guid keys.
    /// </summary>
    private class TestObjectWithGuidKey
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with Guid key and string value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<Guid, string>))]
        public IList<CommonKeyValuePair<Guid, string>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with integer keys.
    /// </summary>
    private class TestObjectWithIntKey
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with integer key and string value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<int, string>))]
        public IList<CommonKeyValuePair<int, string>>? Data { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Test class that uses the converter with integer values.
    /// </summary>
    private class TestObjectWithIntValue
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the data property with string key and integer value.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(CommonKeyValuePairListConverter<string, int>))]
        public IList<CommonKeyValuePair<string, int>>? Data { get; set; }

        #endregion Public Properties
    }

    #endregion Private Classes
}
