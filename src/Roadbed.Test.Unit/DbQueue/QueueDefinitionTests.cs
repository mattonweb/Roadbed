namespace Roadbed.Test.Unit.DbQueue;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.DbQueue;

/// <summary>
/// Unit tests for <see cref="QueueDefinition{T}"/>.
/// </summary>
[TestClass]
public class QueueDefinitionTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the ctor sets the validated name, exposes the supplied
    /// factory, and derives both table names.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidArgs_PopulatesAllProperties()
    {
        // Arrange (Given)
        var factory = new StubConnectionFactory();

        // Act (When)
        var definition = new QueueDefinition<TestPayload>("foo_unsubscribe", factory);

        // Assert (Then)
        Assert.AreEqual("foo_unsubscribe", definition.QueueName);
        Assert.AreSame(factory, definition.ConnectionFactory);
        Assert.AreEqual("queue_message_foo_unsubscribe", definition.MessageTableName);
        Assert.AreEqual("queue_processed_foo_unsubscribe", definition.ProcessedTableName);
        Assert.AreEqual(typeof(TestPayload), definition.PayloadType);
    }

    /// <summary>
    /// Verifies that a null connection factory is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            _ = new QueueDefinition<TestPayload>("foo", connectionFactory: null!);
        }
        catch (ArgumentNullException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Null connection factory must throw.");
    }

    /// <summary>
    /// Verifies that an invalid queue name throws at ctor — before any SQL
    /// could possibly be built — surfacing the safety guard at host startup.
    /// </summary>
    [TestMethod]
    public void Constructor_InvalidName_ThrowsArgumentExceptionImmediately()
    {
        // Arrange (Given)
        var factory = new StubConnectionFactory();
        bool thrown = false;

        // Act (When)
        try
        {
            _ = new QueueDefinition<TestPayload>("Foo;drop", factory);
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Invalid queue name must be rejected at construction, before any SQL is built.");
    }

    #endregion Public Methods

    #region Private Types

    /// <summary>
    /// Test-only stub for <see cref="IDataConnectionFactory"/>; not invoked.
    /// </summary>
    private sealed class StubConnectionFactory : IDataConnectionFactory
    {
        /// <inheritdoc/>
        public DataConnecionString Connecion { get; } =
            new (DataConnectionStringType.SQLiteInMemory) { DatabaseSource = "QueueDefinitionTests" };

        /// <inheritdoc/>
        public IDbConnection CreateOpenConnection()
        {
            throw new InvalidOperationException("Stub: not expected to be called by QueueDefinition tests.");
        }

        /// <inheritdoc/>
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Stub: not expected to be called by QueueDefinition tests.");
        }
    }

    /// <summary>
    /// Test payload type — drives the generic in <see cref="QueueDefinition{T}"/>.
    /// </summary>
    private sealed class TestPayload
    {
    }

    #endregion Private Types
}
