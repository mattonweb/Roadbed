namespace Roadbed.Test.Unit.Logging;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression guards on the <c>job_checkpoint</c> DDL assets Roadbed.Logging
/// ships: the never-prune / never-partition contract, MariaDB's 64-character
/// identifier limit, the column widths a consumer depends on, and that the
/// dated migration creates exactly the table the install script does.
/// </summary>
/// <remarks>
/// Roadbed.Logging copies <c>Assets/Tables/**</c> to its output, and the files
/// flow into this test project's bin folder. Change an asset, rebuild, then run
/// the tests: a <c>--no-build</c> run reads the stale copy.
/// </remarks>
[TestClass]
public sealed class LoggingJobCheckpointDdlTemplateTests
{
    #region Private Fields

    /// <summary>
    /// Directory holding the copied Roadbed.Logging DDL assets.
    /// </summary>
    private static readonly string TablesDirectory =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Tables");

    private static readonly string InstallMySqlPath =
        Path.Combine(TablesDirectory, "job_checkpoint", "install_mysql.txt");

    private static readonly string InstallSqlitePath =
        Path.Combine(TablesDirectory, "job_checkpoint", "install_sqlite.txt");

    private static readonly string UpgradeMySqlPath =
        Path.Combine(TablesDirectory, "upgrade_2026-10_job_checkpoint_mysql.txt");

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the MySQL install script never partitions the
    /// table and states that it is control state that must never be pruned.
    /// </summary>
    [TestMethod]
    public void InstallMySqlTemplate_Table_IsNotPartitionedAndMarkedNeverPrune()
    {
        // Arrange (Given)
        string ddl = ReadTemplate(InstallMySqlPath);

        // Act (When)
        string statements = StripComments(ddl);

        // Assert (Then)
        Assert.DoesNotContain(
            "PARTITION",
            statements.ToUpperInvariant(),
            "job_checkpoint is control state and must never be partitioned.");
        StringAssert.Contains(
            ddl,
            "NEVER PRUNE THIS TABLE",
            "The install script must state that the table is never pruned.");
        StringAssert.Contains(
            ddl,
            "NEVER PARTITION THIS TABLE",
            "The install script must state that the table is never partitioned.");
    }

    /// <summary>
    /// Unit test to verify that the dated migration never partitions the table
    /// and repeats the never-prune notice for the person running it.
    /// </summary>
    [TestMethod]
    public void UpgradeMySqlTemplate_Table_IsNotPartitionedAndMarkedNeverPrune()
    {
        // Arrange (Given)
        string ddl = ReadTemplate(UpgradeMySqlPath);

        // Act (When)
        string statements = StripComments(ddl);

        // Assert (Then)
        Assert.DoesNotContain(
            "PARTITION",
            statements.ToUpperInvariant(),
            "The migration must never partition job_checkpoint.");
        StringAssert.Contains(
            ddl,
            "NEVER prune this table",
            "The migration must state that the table is never pruned.");
    }

    /// <summary>
    /// Unit test to verify that the SQLite install script states that the table
    /// is never pruned.
    /// </summary>
    [TestMethod]
    public void InstallSqliteTemplate_Table_IsMarkedNeverPrune()
    {
        // Arrange (Given)
        string ddl = ReadTemplate(InstallSqlitePath);

        // Act (When)
        string statements = StripComments(ddl);

        // Assert (Then)
        StringAssert.Contains(
            ddl,
            "NEVER PRUNE THIS TABLE",
            "The SQLite install script must state that the table is never pruned.");
        StringAssert.Contains(
            statements,
            "CREATE TABLE job_checkpoint",
            "The SQLite install script must create job_checkpoint.");
    }

    /// <summary>
    /// Unit test to verify that every identifier in the three scripts fits
    /// MariaDB's 64-character limit (a 69-character key name failed with error
    /// 1059 on a live database).
    /// </summary>
    [TestMethod]
    public void AllTemplates_Identifiers_AtMost64Characters()
    {
        // Arrange (Given)
        string[] paths = new[] { InstallMySqlPath, UpgradeMySqlPath, InstallSqlitePath };

        // Act (When)
        string[] tooLong = paths
            .SelectMany(path => Regex
                .Matches(StripComments(ReadTemplate(path)), "[A-Za-z_][A-Za-z0-9_$]*")
                .Select(match => match.Value))
            .Where(identifier => identifier.Length > 64)
            .Distinct()
            .ToArray();

        // Assert (Then)
        Assert.IsEmpty(
            tooLong,
            "Every identifier in the job_checkpoint scripts must be at most 64 characters.");
    }

    /// <summary>
    /// Unit test to verify that the dated migration creates exactly the table the
    /// install script does, so the two can never drift apart.
    /// </summary>
    [TestMethod]
    public void UpgradeMySqlTemplate_CreateStatement_MatchesInstallTemplate()
    {
        // Arrange (Given)
        string install = ExtractCreateStatement(ReadTemplate(InstallMySqlPath));
        string upgrade = ExtractCreateStatement(ReadTemplate(UpgradeMySqlPath))
            .Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE", StringComparison.Ordinal);

        // Act (When)
        bool identical = string.Equals(install, upgrade, StringComparison.Ordinal);

        // Assert (Then)
        Assert.IsTrue(
            identical,
            "The migration's CREATE TABLE must match job_checkpoint/install_mysql.txt column for column.");
    }

    /// <summary>
    /// Unit test to verify the MySQL column contract a consumer depends on: the
    /// five-column primary key, a NOT NULL environment, and a fire instance id as
    /// wide as Quartz.NET's own QRTZ_FIRED_TRIGGERS.ENTRY_ID.
    /// </summary>
    [TestMethod]
    public void InstallMySqlTemplate_Columns_MatchConsumerContract()
    {
        // Arrange (Given)
        string statements = Regex.Replace(StripComments(ReadTemplate(InstallMySqlPath)), @"\s+", " ");

        // Act (When)
        string[] expectedFragments = new[]
        {
            "PRIMARY KEY (application, environment, job_group, job_name, checkpoint_key)",
            "environment VARCHAR(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL",
            "advanced_by_fire_instance_id VARCHAR(140) NULL",
            "position_text VARCHAR(1024)",
            "checkpoint_key VARCHAR(100)",
            "version BIGINT NOT NULL DEFAULT 0",
        };

        // Assert (Then)
        foreach (string fragment in expectedFragments)
        {
            StringAssert.Contains(
                statements,
                fragment,
                "The job_checkpoint MySQL columns must match the documented consumer contract.");
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Returns the CREATE TABLE statement (through its closing semicolon) with
    /// whitespace runs collapsed.
    /// </summary>
    /// <param name="ddl">The full script.</param>
    /// <returns>The normalized CREATE TABLE statement.</returns>
    private static string ExtractCreateStatement(string ddl)
    {
        string statements = StripComments(ddl);
        int start = statements.IndexOf("CREATE TABLE", StringComparison.Ordinal);
        int end = statements.IndexOf(';', start);
        return Regex.Replace(statements.Substring(start, end - start + 1), @"\s+", " ");
    }

    /// <summary>
    /// Reads a copied DDL asset, failing with a clear message when it is missing.
    /// </summary>
    /// <param name="path">The asset path under the test bin folder.</param>
    /// <returns>The script text.</returns>
    private static string ReadTemplate(string path)
    {
        Assert.IsTrue(
            File.Exists(path),
            "The job_checkpoint DDL asset must be copied to the test output. Rebuild Roadbed.Logging.");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Removes <c>--</c> comment lines so only SQL statements remain.
    /// </summary>
    /// <param name="ddl">The full script.</param>
    /// <returns>The script without comment lines.</returns>
    private static string StripComments(string ddl)
    {
        return string.Join(
            "\n",
            ddl.Split('\n').Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));
    }

    #endregion Private Methods
}
