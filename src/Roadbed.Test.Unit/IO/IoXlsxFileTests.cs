namespace Roadbed.Test.Unit.IO;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.IO;

/// <summary>
/// Unit tests for <see cref="IoXlsxFile{T}"/>. Fixtures are authored at runtime
/// with ClosedXML (test-only) and read back through the Sylvan-backed
/// streaming reader.
/// </summary>
[TestClass]
public class IoXlsxFileTests
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the MSTest context.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// A headered single-sheet workbook maps to typed rows by column name.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task ReadRowsAsync_HeaderedSheet_MapsByColumnName()
    {
        // Arrange (Given)
        string path = CreatePlacesWorkbook();

        try
        {
            var file = IoXlsxFile<PlaceRow>.FromFile(path, new HeaderedPlaceMapper());

            // Act (When)
            List<PlaceRow> rows = await CollectAsync(file.ReadRowsAsync(this.Token));

            // Assert (Then)
            Assert.AreEqual(2, rows.Count, "The blank-name row must be filtered by the mapper.");
            Assert.AreEqual("01001", rows[0].Fips, "A leading-zero text code must round-trip when stored as text.");
            Assert.AreEqual("Alpha", rows[0].Name);
            Assert.AreEqual(100, rows[0].Population, "Numeric cell read as string then parsed by the mapper.");
            Assert.AreEqual("01002", rows[1].Fips);
        }
        finally
        {
            Cleanup(path);
        }
    }

    /// <summary>
    /// A worksheet can be selected by name; an unknown name throws a clear error.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task ReadRowsAsync_BySheetName_SelectsThatSheet_AndUnknownThrows()
    {
        // Arrange (Given)
        string path = CreateTwoSheetWorkbook();

        try
        {
            var file = IoXlsxFile<PlaceRow>.FromFile(
                path,
                new HeaderedPlaceMapper(),
                new IoXlsxReadOptions { SheetName = "Second" });

            // Act (When)
            List<PlaceRow> rows = await CollectAsync(file.ReadRowsAsync(this.Token));

            // Assert (Then)
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Second-Only", rows[0].Name, "Rows must come from the named sheet.");

            var missing = IoXlsxFile<PlaceRow>.FromFile(
                path,
                new HeaderedPlaceMapper(),
                new IoXlsxReadOptions { SheetName = "DoesNotExist" });

            ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await CollectAsync(missing.ReadRowsAsync(this.Token)));
            StringAssert.Contains(ex.Message, "DoesNotExist", "Error should name the missing worksheet.");
        }
        finally
        {
            Cleanup(path);
        }
    }

    /// <summary>
    /// A worksheet can be selected by zero-based index.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task ReadRowsAsync_BySheetIndex_SelectsThatSheet()
    {
        // Arrange (Given)
        string path = CreateTwoSheetWorkbook();

        try
        {
            var file = IoXlsxFile<PlaceRow>.FromFile(
                path,
                new HeaderedPlaceMapper(),
                new IoXlsxReadOptions { SheetIndex = 1 });

            // Act (When)
            List<PlaceRow> rows = await CollectAsync(file.ReadRowsAsync(this.Token));

            // Assert (Then)
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Second-Only", rows[0].Name);
        }
        finally
        {
            Cleanup(path);
        }
    }

    /// <summary>
    /// A headerless sheet with banner rows maps by ordinal after skipping.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task ReadRowsAsync_HeaderlessWithBannerRows_SkipsThenMapsByOrdinal()
    {
        // Arrange (Given) — 2 banner rows, then a header-ish row, then data.
        string path = CreateBannerWorkbook();

        try
        {
            var file = IoXlsxFile<PlaceRow>.FromFile(
                path,
                new OrdinalPlaceMapper(),
                new IoXlsxReadOptions { HasHeaders = false, SkipLeadingRows = 3 });

            // Act (When)
            List<PlaceRow> rows = await CollectAsync(file.ReadRowsAsync(this.Token));

            // Assert (Then)
            Assert.AreEqual(2, rows.Count, "Only the two data rows after the 3 skipped rows should map.");
            Assert.AreEqual("Gamma", rows[0].Name);
            Assert.AreEqual("Delta", rows[1].Name);
        }
        finally
        {
            Cleanup(path);
        }
    }

    /// <summary>
    /// Headered + SkipLeadingRows is rejected at construction (the documented contract).
    /// </summary>
    [TestMethod]
    public void FromFile_HeaderedWithSkipLeadingRows_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            IoXlsxFile<PlaceRow>.FromFile(
                "ignored.xlsx",
                new HeaderedPlaceMapper(),
                new IoXlsxReadOptions { HasHeaders = true, SkipLeadingRows = 2 }));
    }

    /// <summary>
    /// ReadBatchesAsync yields full batches plus a final partial batch, and its
    /// total equals ReadRowsAsync.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task ReadBatchesAsync_BatchesRows_WithFinalPartialBatch()
    {
        // Arrange (Given) — 5 mappable rows.
        string path = CreateManyRowsWorkbook(rowCount: 5);

        try
        {
            var file = IoXlsxFile<PlaceRow>.FromFile(path, new HeaderedPlaceMapper());

            // Act (When)
            var batches = new List<IReadOnlyList<PlaceRow>>();
            await foreach (IReadOnlyList<PlaceRow> batch in file.ReadBatchesAsync(2, this.Token))
            {
                batches.Add(batch);
            }

            // Assert (Then)
            Assert.AreEqual(3, batches.Count, "5 rows in batches of 2 => 2 + 2 + 1.");
            Assert.AreEqual(2, batches[0].Count);
            Assert.AreEqual(2, batches[1].Count);
            Assert.AreEqual(1, batches[2].Count, "Final partial batch.");
            Assert.AreEqual(5, batches.Sum(b => b.Count));
        }
        finally
        {
            Cleanup(path);
        }
    }

    #endregion Public Methods

    #region Private Properties

    private CancellationToken Token => this.TestContext.CancellationToken;

    #endregion Private Properties

    #region Private Methods

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (T item in source)
        {
            list.Add(item);
        }

        return list;
    }

    private static string CreatePlacesWorkbook()
    {
        return CreateWorkbook(wb =>
        {
            IXLWorksheet ws = wb.AddWorksheet("Places");
            ws.Cell(1, 1).Value = "Fips";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Population";

            ws.Cell(2, 1).SetValue("01001");
            ws.Cell(2, 2).SetValue("Alpha");
            ws.Cell(2, 3).Value = 100;

            ws.Cell(3, 1).SetValue("01002");
            ws.Cell(3, 2).SetValue("Beta");
            ws.Cell(3, 3).Value = 200;

            // Blank name -> mapper returns null -> filtered out.
            ws.Cell(4, 1).SetValue("01003");
            ws.Cell(4, 2).SetValue(string.Empty);
            ws.Cell(4, 3).Value = 300;
        });
    }

    private static string CreateTwoSheetWorkbook()
    {
        return CreateWorkbook(wb =>
        {
            IXLWorksheet first = wb.AddWorksheet("First");
            first.Cell(1, 1).Value = "Fips";
            first.Cell(1, 2).Value = "Name";
            first.Cell(1, 3).Value = "Population";
            first.Cell(2, 1).SetValue("10000");
            first.Cell(2, 2).SetValue("First-Only");
            first.Cell(2, 3).Value = 1;

            IXLWorksheet second = wb.AddWorksheet("Second");
            second.Cell(1, 1).Value = "Fips";
            second.Cell(1, 2).Value = "Name";
            second.Cell(1, 3).Value = "Population";
            second.Cell(2, 1).SetValue("20000");
            second.Cell(2, 2).SetValue("Second-Only");
            second.Cell(2, 3).Value = 2;
        });
    }

    private static string CreateBannerWorkbook()
    {
        return CreateWorkbook(wb =>
        {
            IXLWorksheet ws = wb.AddWorksheet("Report");
            ws.Cell(1, 1).SetValue("Agency banner line");
            ws.Cell(2, 1).SetValue("Generated 2026-06-14");
            ws.Cell(3, 1).SetValue("Fips");
            ws.Cell(3, 2).SetValue("Name");
            ws.Cell(3, 3).SetValue("Population");
            ws.Cell(4, 1).SetValue("30001");
            ws.Cell(4, 2).SetValue("Gamma");
            ws.Cell(4, 3).Value = 10;
            ws.Cell(5, 1).SetValue("30002");
            ws.Cell(5, 2).SetValue("Delta");
            ws.Cell(5, 3).Value = 20;
        });
    }

    private static string CreateManyRowsWorkbook(int rowCount)
    {
        return CreateWorkbook(wb =>
        {
            IXLWorksheet ws = wb.AddWorksheet("Places");
            ws.Cell(1, 1).Value = "Fips";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Population";

            for (int i = 0; i < rowCount; i++)
            {
                ws.Cell(i + 2, 1).SetValue((40000 + i).ToString());
                ws.Cell(i + 2, 2).SetValue("Row" + i);
                ws.Cell(i + 2, 3).Value = i;
            }
        });
    }

    private static string CreateWorkbook(Action<XLWorkbook> build)
    {
        string path = Path.Combine(Path.GetTempPath(), "roadbed_xlsx_" + Guid.NewGuid().ToString("N") + ".xlsx");
        using (var wb = new XLWorkbook())
        {
            build(wb);
            wb.SaveAs(path);
        }

        return path;
    }

    private static void Cleanup(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort test cleanup.
        }
    }

    #endregion Private Methods

    #region Private Types

    private sealed class PlaceRow
    {
        public string Fips { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Population { get; set; }
    }

    /// <summary>
    /// Reads by column name (headered) and filters blank-name rows.
    /// </summary>
    private sealed class HeaderedPlaceMapper : IXlsxEntityMapper<PlaceRow>
    {
        public PlaceRow? MapEntity(DbDataReader reader)
        {
            string name = ReadString(reader, reader.GetOrdinal("Name"));
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new PlaceRow
            {
                Fips = ReadString(reader, reader.GetOrdinal("Fips")),
                Name = name,
                Population = int.TryParse(ReadString(reader, reader.GetOrdinal("Population")), out int pop) ? pop : 0,
            };
        }
    }

    /// <summary>
    /// Reads by ordinal (headerless).
    /// </summary>
    private sealed class OrdinalPlaceMapper : IXlsxEntityMapper<PlaceRow>
    {
        public PlaceRow? MapEntity(DbDataReader reader)
        {
            string name = ReadString(reader, 1);
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new PlaceRow
            {
                Fips = ReadString(reader, 0),
                Name = name,
                Population = int.TryParse(ReadString(reader, 2), out int pop) ? pop : 0,
            };
        }
    }

    #endregion Private Types

    #region Private Helpers

    private static string ReadString(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    #endregion Private Helpers
}
