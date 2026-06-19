namespace Roadbed.Test.Unit.Data.Dapper;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using global::Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Contains unit tests for verifying the behavior of the DapperDateOnlyHandler class.
/// </summary>
[TestClass]
public class DapperDateOnlyHandlerTests
{
    #region Public Properties

    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext? TestContext { get; set; }

    #endregion Public Properties

    #region Public Methods

    #region Parse Tests

    /// <summary>
    /// Unit test to verify that Parse converts an ISO 8601 date string to DateOnly.
    /// </summary>
    [TestMethod]
    public void Parse_IsoDateStringValue_ReturnsDateOnly()
    {
        // Arrange (Given)
        var handler = new DapperDateOnlyHandler();
        string textValue = "2026-06-15";

        // Act (When)
        DateOnly result = handler.Parse(textValue);

        // Assert (Then)
        Assert.AreEqual(
            new DateOnly(2026, 6, 15),
            result,
            "Parsed DateOnly should match the input ISO 8601 date.");
    }

    /// <summary>
    /// Unit test to verify that Parse drops the time component when given a DateTime.
    /// </summary>
    [TestMethod]
    public void Parse_DateTimeValue_DropsTimeComponent()
    {
        // Arrange (Given)
        var handler = new DapperDateOnlyHandler();
        DateTime dateTime = new DateTime(2026, 6, 15, 14, 30, 0);

        // Act (When)
        DateOnly result = handler.Parse(dateTime);

        // Assert (Then)
        Assert.AreEqual(
            new DateOnly(2026, 6, 15),
            result,
            "Parsed DateOnly should match the input DateTime's date component.");
    }

    /// <summary>
    /// Unit test to verify that Parse returns an existing DateOnly unchanged.
    /// </summary>
    [TestMethod]
    public void Parse_DateOnlyValue_ReturnsUnchanged()
    {
        // Arrange (Given)
        var handler = new DapperDateOnlyHandler();
        DateOnly input = new DateOnly(2026, 6, 15);

        // Act (When)
        DateOnly result = handler.Parse(input);

        // Assert (Then)
        Assert.AreEqual(
            input,
            result,
            "DateOnly input should be returned unchanged.");
    }

    /// <summary>
    /// Unit test to verify that Parse throws exception for invalid type.
    /// </summary>
    [TestMethod]
    public void Parse_InvalidType_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var handler = new DapperDateOnlyHandler();
        int invalidValue = 12345;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            handler.Parse(invalidValue);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Parse should throw InvalidOperationException for invalid type.");
    }

    #endregion Parse Tests

    #region SetValue Tests

    /// <summary>
    /// Unit test to verify that SetValue writes a DateOnly as a yyyy-MM-dd string with DbType.Date.
    /// </summary>
    [TestMethod]
    public void SetValue_DateOnly_SetsIsoDateStringAndDbTypeDate()
    {
        // Arrange (Given)
        var handler = new DapperDateOnlyHandler();
        DateOnly value = new DateOnly(2026, 6, 15);
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, value);

        // Assert (Then)
        Assert.AreEqual(
            "2026-06-15",
            parameter.Value,
            "Parameter value should be the ISO 8601 yyyy-MM-dd string.");
        Assert.AreEqual(
            DbType.Date,
            parameter.DbType,
            "Parameter DbType should be Date.");
    }

    #endregion SetValue Tests

    #region Integration Tests

    /// <summary>
    /// Unit test to verify that handler correctly round-trips DateOnly through SQLite.
    /// </summary>
    [TestMethod]
    public void Integration_RoundTripDateOnly_PreservesValue()
    {
        // Arrange (Given)
        SqlMapper.AddTypeHandler(new DapperDateOnlyHandler());
        DapperMapping.Configure(typeof(TestDateOnlyDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateOnly originalDate = new DateOnly(2026, 6, 15);

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, terms_version TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, terms_version) VALUES (@Id, @TermsVersion)",
                new { Id = 1, TermsVersion = originalDate });

            var result = connection.QuerySingleOrDefault<TestDateOnlyDto>(
                "SELECT id, terms_version FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                originalDate,
                result!.TermsVersion,
                "DateOnly should round-trip correctly.");
        }
    }

    #endregion Integration Tests

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a connection factory with a unique in-memory database.
    /// </summary>
    /// <returns>Connection factory for testing.</returns>
    private IDataConnectionFactory CreateConnectionFactory()
    {
        string uniqueDbName = $"TestDb_{Guid.NewGuid():N}";
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory)
        {
            DatabaseSource = uniqueDbName,
        };
        return new SqliteConnectionFactory(connectionString);
    }

    #endregion Private Methods

    #region Test DTOs

    /// <summary>
    /// Test DTO for DateOnly integration tests.
    /// </summary>
    private class TestDateOnlyDto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("terms_version")]
        public DateOnly TermsVersion { get; set; }
    }

    #endregion Test DTOs
}
