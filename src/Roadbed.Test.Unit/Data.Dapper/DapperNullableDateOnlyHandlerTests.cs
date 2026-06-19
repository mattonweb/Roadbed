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
/// Contains unit tests for verifying the behavior of the DapperNullableDateOnlyHandler class.
/// </summary>
[TestClass]
public class DapperNullableDateOnlyHandlerTests
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
    /// Unit test to verify that Parse returns null for DBNull value.
    /// </summary>
    [TestMethod]
    public void Parse_DbNullValue_ReturnsNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        object dbNullValue = DBNull.Value;

        // Act (When)
        DateOnly? result = handler.Parse(dbNullValue);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Parse should return null for DBNull value.");
    }

    /// <summary>
    /// Unit test to verify that Parse returns null for null value.
    /// </summary>
    [TestMethod]
    public void Parse_NullValue_ReturnsNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        object? nullValue = null;

        // Act (When)
        DateOnly? result = handler.Parse(nullValue!);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Parse should return null for null value.");
    }

    /// <summary>
    /// Unit test to verify that Parse converts an ISO 8601 date string to DateOnly.
    /// </summary>
    [TestMethod]
    public void Parse_IsoDateStringValue_ReturnsDateOnly()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        string textValue = "2026-06-15";

        // Act (When)
        DateOnly? result = handler.Parse(textValue);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateOnly value.");
        Assert.AreEqual(
            new DateOnly(2026, 6, 15),
            result!.Value,
            "Parsed DateOnly should match the input ISO 8601 date.");
    }

    /// <summary>
    /// Unit test to verify that Parse drops the time component when given a DateTime.
    /// </summary>
    [TestMethod]
    public void Parse_DateTimeValue_DropsTimeComponent()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        DateTime dateTime = new DateTime(2026, 6, 15, 14, 30, 0);

        // Act (When)
        DateOnly? result = handler.Parse(dateTime);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateOnly value.");
        Assert.AreEqual(
            new DateOnly(2026, 6, 15),
            result!.Value,
            "Parsed DateOnly should match the input DateTime's date component.");
    }

    /// <summary>
    /// Unit test to verify that Parse returns an existing DateOnly unchanged.
    /// </summary>
    [TestMethod]
    public void Parse_DateOnlyValue_ReturnsUnchanged()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        DateOnly input = new DateOnly(2026, 6, 15);

        // Act (When)
        DateOnly? result = handler.Parse(input);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateOnly value.");
        Assert.AreEqual(
            input,
            result!.Value,
            "DateOnly input should be returned unchanged.");
    }

    /// <summary>
    /// Unit test to verify that Parse throws exception for invalid type.
    /// </summary>
    [TestMethod]
    public void Parse_InvalidType_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
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
    /// Unit test to verify that SetValue sets DBNull for null DateOnly.
    /// </summary>
    [TestMethod]
    public void SetValue_NullDateOnly_SetsDbNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        DateOnly? nullDate = null;
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, nullDate);

        // Assert (Then)
        Assert.AreEqual(
            DBNull.Value,
            parameter.Value,
            "Parameter value should be DBNull for null DateOnly.");
    }

    /// <summary>
    /// Unit test to verify that SetValue writes a DateOnly as a yyyy-MM-dd string with DbType.Date.
    /// </summary>
    [TestMethod]
    public void SetValue_DateOnly_SetsIsoDateStringAndDbTypeDate()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateOnlyHandler();
        DateOnly? value = new DateOnly(2026, 6, 15);
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
        SqlMapper.AddTypeHandler(new DapperNullableDateOnlyHandler());
        DapperMapping.Configure(typeof(TestNullableDateOnlyDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateOnly? originalDate = new DateOnly(2026, 6, 15);

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, terms_version TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, terms_version) VALUES (@Id, @TermsVersion)",
                new { Id = 1, TermsVersion = originalDate });

            var result = connection.QuerySingleOrDefault<TestNullableDateOnlyDto>(
                "SELECT id, terms_version FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.IsNotNull(
                result!.TermsVersion,
                "TermsVersion should not be null.");
            Assert.AreEqual(
                originalDate.Value,
                result.TermsVersion!.Value,
                "DateOnly should round-trip correctly.");
        }
    }

    /// <summary>
    /// Unit test to verify that handler correctly round-trips null DateOnly through SQLite.
    /// </summary>
    [TestMethod]
    public void Integration_RoundTripNullDateOnly_PreservesNull()
    {
        // Arrange (Given)
        SqlMapper.AddTypeHandler(new DapperNullableDateOnlyHandler());
        DapperMapping.Configure(typeof(TestNullableDateOnlyDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateOnly? nullDate = null;

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, terms_version TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, terms_version) VALUES (@Id, @TermsVersion)",
                new { Id = 1, TermsVersion = nullDate });

            var result = connection.QuerySingleOrDefault<TestNullableDateOnlyDto>(
                "SELECT id, terms_version FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.IsNull(
                result!.TermsVersion,
                "TermsVersion should be null after round-trip.");
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
    /// Test DTO for nullable DateOnly integration tests.
    /// </summary>
    private class TestNullableDateOnlyDto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("terms_version")]
        public DateOnly? TermsVersion { get; set; }
    }

    #endregion Test DTOs
}
