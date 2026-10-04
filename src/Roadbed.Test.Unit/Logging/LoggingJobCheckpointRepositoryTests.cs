namespace Roadbed.Test.Unit.Logging;

using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Logging;

/// <summary>
/// Contains unit tests for verifying the SQL that <see cref="LoggingJobCheckpointRepository"/>
/// emits, for the MariaDB behavior the in-memory SQLite tests cannot prove.
/// </summary>
[TestClass]
public sealed class LoggingJobCheckpointRepositoryTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the advance assigns every previous_position_*
    /// column before the matching position_* column. MariaDB evaluates
    /// single-table UPDATE assignments left to right, so the other order would
    /// copy the new position into the previous columns; SQLite would not catch it.
    /// </summary>
    [TestMethod]
    public void BuildAdvanceSql_AnyTable_AssignsPreviousPositionBeforePosition()
    {
        // Arrange (Given)
        string[] cursors = new[] { "id", "on", "text" };

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildAdvanceSql("logging.job_checkpoint"));

        // Assert (Then)
        foreach (string cursor in cursors)
        {
            int previousAssignment = sql.IndexOf($"previous_position_{cursor} = position_{cursor}", StringComparison.Ordinal);
            int positionAssignment = sql.IndexOf($",position_{cursor} = @Position", StringComparison.Ordinal);

            Assert.IsGreaterThanOrEqualTo(
                0,
                previousAssignment,
                "The advance must copy each current cursor into its previous_position_* column.");
            Assert.IsGreaterThan(
                previousAssignment,
                positionAssignment,
                "Each previous_position_* assignment must come before the position_* assignment it copies.");
        }
    }

    /// <summary>
    /// Unit test to verify that the advance is a compare-and-set on the version:
    /// it matches only the version the caller read and moves it by one.
    /// </summary>
    [TestMethod]
    public void BuildAdvanceSql_AnyTable_ComparesAndIncrementsVersion()
    {
        // Arrange (Given)
        string tableRef = "job_checkpoint";

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildAdvanceSql(tableRef));

        // Assert (Then)
        StringAssert.Contains(
            sql,
            "AND version = @ExpectedVersion",
            "The advance must only match the version the caller read.");
        StringAssert.Contains(
            sql,
            "version = version + 1",
            "The advance must move the version by one.");
    }

    /// <summary>
    /// Unit test to verify that the advance is pinned to one row by all five
    /// primary-key columns.
    /// </summary>
    [TestMethod]
    public void BuildAdvanceSql_AnyTable_FiltersOnFullPrimaryKey()
    {
        // Arrange (Given)
        string[] keyPredicates = new[]
        {
            "application = @Application",
            "AND environment = @Environment",
            "AND job_group = @JobGroup",
            "AND job_name = @JobName",
            "AND checkpoint_key = @CheckpointKey",
        };

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildAdvanceSql("job_checkpoint"));

        // Assert (Then)
        foreach (string predicate in keyPredicates)
        {
            StringAssert.Contains(
                sql,
                predicate,
                "The advance must filter on every primary-key column.");
        }
    }

    /// <summary>
    /// Unit test to verify that the MySQL "insert when absent" uses a no-op
    /// ON DUPLICATE KEY UPDATE and never INSERT IGNORE, which would turn a
    /// truncation or NOT NULL failure into a silent warning.
    /// </summary>
    [TestMethod]
    public void BuildEnsureExistsSql_MySql_UsesNoOpDuplicateKeyUpdate()
    {
        // Arrange (Given)
        DataConnectionStringType type = DataConnectionStringType.MySQL;

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildEnsureExistsSql(type, "job_checkpoint"));

        // Assert (Then)
        StringAssert.Contains(
            sql,
            "ON DUPLICATE KEY UPDATE application = application",
            "MySQL must leave an existing row unchanged with a no-op duplicate-key update.");
        Assert.DoesNotContain(
            "IGNORE",
            sql,
            "The insert must never use INSERT IGNORE.");
    }

    /// <summary>
    /// Unit test to verify that the SQLite "insert when absent" ignores only the
    /// primary-key conflict and never uses INSERT OR IGNORE, which would also
    /// hide NOT NULL failures.
    /// </summary>
    [TestMethod]
    public void BuildEnsureExistsSql_Sqlite_UsesOnConflictDoNothing()
    {
        // Arrange (Given)
        DataConnectionStringType type = DataConnectionStringType.SQLite;

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildEnsureExistsSql(type, "job_checkpoint"));

        // Assert (Then)
        StringAssert.Contains(
            sql,
            "ON CONFLICT (application, environment, job_group, job_name, checkpoint_key) DO NOTHING",
            "SQLite must ignore only the primary-key conflict.");
        Assert.DoesNotContain(
            "IGNORE",
            sql,
            "The insert must never use INSERT OR IGNORE.");
    }

    /// <summary>
    /// Unit test to verify that the MySQL run-start upsert's update branch sets
    /// only last_run_started_on and last_modified_on, never the version, the
    /// position or advanced_on.
    /// </summary>
    [TestMethod]
    public void BuildMarkRunStartedSql_MySql_UpdatesOnlyRunStartColumns()
    {
        // Arrange (Given)
        DataConnectionStringType type = DataConnectionStringType.MySQL;

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildMarkRunStartedSql(type, "job_checkpoint"));
        string updateBranch = sql.Substring(sql.IndexOf("ON DUPLICATE KEY UPDATE", StringComparison.Ordinal));

        // Assert (Then)
        AssertUpdatesOnlyRunStartColumns(updateBranch);
    }

    /// <summary>
    /// Unit test to verify that the SQLite run-start upsert's update branch sets
    /// only last_run_started_on and last_modified_on, never the version, the
    /// position or advanced_on.
    /// </summary>
    [TestMethod]
    public void BuildMarkRunStartedSql_Sqlite_UpdatesOnlyRunStartColumns()
    {
        // Arrange (Given)
        DataConnectionStringType type = DataConnectionStringType.SQLiteInMemory;

        // Act (When)
        string sql = Normalize(LoggingJobCheckpointRepository.BuildMarkRunStartedSql(type, "job_checkpoint"));
        string updateBranch = sql.Substring(sql.IndexOf("DO UPDATE SET", StringComparison.Ordinal));

        // Assert (Then)
        AssertUpdatesOnlyRunStartColumns(updateBranch);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Asserts that an upsert's update branch touches only the run-start columns.
    /// </summary>
    /// <param name="updateBranch">The SQL from the start of the update branch.</param>
    private static void AssertUpdatesOnlyRunStartColumns(string updateBranch)
    {
        StringAssert.Contains(
            updateBranch,
            "last_run_started_on =",
            "The update branch must record the run start.");
        Assert.DoesNotContain(
            "version",
            updateBranch,
            "Recording a run start must never touch the version.");
        Assert.DoesNotContain(
            "position",
            updateBranch,
            "Recording a run start must never touch the position.");
        Assert.DoesNotContain(
            "advanced",
            updateBranch,
            "Recording a run start must never touch advanced_on or advanced_by_fire_instance_id.");
    }

    /// <summary>
    /// Collapses every run of whitespace to one space so assertions do not depend
    /// on the SQL's column alignment.
    /// </summary>
    /// <param name="sql">The SQL to normalize.</param>
    /// <returns>The normalized SQL.</returns>
    private static string Normalize(string sql)
    {
        return Regex.Replace(sql, @"\s+", " ");
    }

    #endregion Private Methods
}
