namespace Roadbed.Test.Unit.DbQueue;

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression guards on the Roadbed.DbQueue.MySql DDL template assets.
/// </summary>
/// <remarks>
/// <para>
/// These tests close the gap that let the original
/// <c>external_id CHAR(36)</c> declaration ship: a consumer running
/// MySqlConnector with the default <c>GuidFormat=Char36</c> connection-string
/// option had every claim throw "Object must implement IConvertible" because
/// the driver materialized the CHAR(36) column as <see cref="Guid"/>, which
/// Dapper could not assign to the <c>string</c> property on
/// <c>ClaimedMessageRow.ExternalId</c>. The queue stayed stuck on row 1.
/// </para>
/// <para>
/// The fix is structural: declare the column as
/// <c>VARCHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL</c>.
/// VARCHAR falls into a different <c>MySqlConnector</c> TypeMapper branch
/// (<c>MYSQL_TYPE_VAR_STRING</c>) and is never coerced to
/// <see cref="Guid"/>. The tests below assert the column declaration in the
/// shipped install template AND that the one-time ALTER migration script
/// exists for already-deployed queues.
/// </para>
/// </remarks>
[TestClass]
public class DbQueueDdlTemplateTests
{
    #region Private Fields

    /// <summary>
    /// Directory holding the linked copies of the satellite's DDL templates,
    /// populated by the test project's csproj via
    /// <c>&lt;None Include="..\Roadbed.DbQueue.MySql\Assets\Tables\**\*.txt" LinkBase="DbQueue\Assets\Tables" /&gt;</c>.
    /// </summary>
    private static readonly string TemplatesDirectory =
        Path.Combine(AppContext.BaseDirectory, "DbQueue", "Assets", "Tables");

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Verifies that the message-table install template declares
    /// <c>external_id</c> as VARCHAR(36) ascii ascii_bin, NOT CHAR(36) —
    /// preventing MySqlConnector's default <c>GuidFormat=Char36</c> from
    /// coercing the column to <see cref="Guid"/> and breaking the claim
    /// path.
    /// </summary>
    [TestMethod]
    public void QueueMessageInstallTemplate_ExternalIdColumn_DeclaredAsVarchar36AsciiBin()
    {
        // Arrange (Given)
        string templatePath = Path.Combine(
            TemplatesDirectory,
            "queue_message",
            "install_mysql.txt");
        Assert.IsTrue(
            File.Exists(templatePath),
            $"queue_message install template must be present at the linked path: {templatePath}");

        // Act (When)
        string ddl = File.ReadAllText(templatePath);

        // Assert (Then)
        StringAssert.Contains(
            ddl,
            "external_id VARCHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL",
            "external_id must be declared as VARCHAR(36) ascii_bin so MySqlConnector's default GuidFormat=Char36 does not coerce the column to System.Guid. See Roadbed.DbQueue.MySql/Assets/Tables/queue_message/install_mysql.txt for the rationale.");

        Assert.IsFalse(
            ddl.Contains("external_id CHAR(36)", StringComparison.Ordinal),
            "external_id must NOT be declared as CHAR(36). Under MySqlConnector's default GuidFormat=Char36 the driver returns CHAR(36) columns as System.Guid and Dapper fails to assign that to ClaimedMessageRow.ExternalId (string). Use VARCHAR(36) instead.");
    }

    /// <summary>
    /// Verifies that the one-time ALTER migration script for already-deployed
    /// queues is present and targets the right MODIFY.
    /// </summary>
    [TestMethod]
    public void ExternalIdUpgradeScript_PresentAndAltersToVarchar36()
    {
        // Arrange (Given)
        string upgradePath = Path.Combine(
            TemplatesDirectory,
            "upgrade_2026-06_external_id_varchar_mysql.txt");
        Assert.IsTrue(
            File.Exists(upgradePath),
            $"One-time upgrade ALTER must be present at the linked path: {upgradePath}");

        // Act (When)
        string sql = File.ReadAllText(upgradePath);

        // Assert (Then)
        StringAssert.Contains(
            sql,
            "ALTER TABLE queue_message_{q}",
            "Upgrade must target the message table per queue (placeholder {q} substituted by the DBA).");
        StringAssert.Contains(
            sql,
            "MODIFY external_id VARCHAR(36)",
            "Upgrade must MODIFY external_id to VARCHAR(36).");
        StringAssert.Contains(
            sql,
            "ascii_bin",
            "Upgrade must preserve the ascii_bin collation so UUIDv7 lexical-equals-chronological ordering survives.");
    }

    /// <summary>
    /// Verifies the processed-table template does NOT carry an external_id
    /// column — it never has, and this test is here so a future "add
    /// external_id to processed" change is a flagged decision rather than a
    /// silent reintroduction of the same Char36 coercion bug.
    /// </summary>
    [TestMethod]
    public void QueueProcessedInstallTemplate_DoesNotDeclareExternalIdColumn()
    {
        // Arrange (Given)
        string templatePath = Path.Combine(
            TemplatesDirectory,
            "queue_processed",
            "install_mysql.txt");
        Assert.IsTrue(File.Exists(templatePath));

        // Act (When)
        string ddl = File.ReadAllText(templatePath);

        // Assert (Then)
        Assert.IsFalse(
            ddl.Contains("external_id", StringComparison.Ordinal),
            "queue_processed_{q} must not carry an external_id column. fk_queue_id is the logical reference back to the message row; surfacing external_id here would re-introduce the CHAR(36)/GuidFormat=Char36 coercion risk that the queue_message template carefully avoids.");
    }

    #endregion Public Methods
}
