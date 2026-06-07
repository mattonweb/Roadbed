namespace Roadbed.Test.Unit.Logging;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Logging;

/// <summary>
/// Unit tests covering the SQL-shape decisions in
/// <see cref="LoggingActivityRepository"/> that drive MySQL partition
/// pruning behavior.
/// </summary>
[TestClass]
public class LoggingActivityRepositoryTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that <see cref="LoggingActivityRepository.BuildWhereClause"/>
    /// includes <c>AND created_on = @CreatedOn</c> when the caller supplies
    /// a value, so MySQL can prune the UPDATE to one monthly partition.
    /// </summary>
    [TestMethod]
    public void BuildWhereClause_CreatedOnSupplied_IncludesAndClause()
    {
        // Arrange (Given)
        DateTime createdOn = new (2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act (When)
        string clause = LoggingActivityRepository.BuildWhereClause(createdOn);

        // Assert (Then)
        StringAssert.Contains(clause, "id = @ActivityId");
        StringAssert.Contains(clause, "AND created_on = @CreatedOn");
    }

    /// <summary>
    /// Verifies that <see cref="LoggingActivityRepository.BuildWhereClause"/>
    /// produces an id-only WHERE clause when the caller does not have a
    /// <c>created_on</c> to pass — the legacy path that probes every
    /// partition.
    /// </summary>
    [TestMethod]
    public void BuildWhereClause_CreatedOnNull_IdOnlyClause()
    {
        // Arrange (Given) + Act (When)
        string clause = LoggingActivityRepository.BuildWhereClause(createdOn: null);

        // Assert (Then)
        StringAssert.Contains(clause, "id = @ActivityId");
        Assert.IsFalse(
            clause.Contains("created_on", StringComparison.Ordinal),
            "When createdOn is null the WHERE clause must not reference created_on so existing legacy callers still match the row.");
    }

    #endregion Public Methods
}
