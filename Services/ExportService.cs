using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Services
{
    /// <summary>
    /// Exports analysis results to Excel (.xlsx), CSV, or PAC CLI script.
    /// The Excel export builds a real Open XML .xlsx package using only
    /// System.IO.Compression — zero NuGet dependencies.
    /// </summary>
    public static class ExportService
    {
        // ── Column headers (order matches the grid) ───────────────────────
        private static readonly string[] Headers =
        {
            "Component ID", "Name", "Type Code", "Type Label",
            "Lifecycle", "Category", "Risk",
            "Source Version", "Target Version",
            "Missing Patches", "Last Target Patch", "Note"
        };

        // ── Style indexes (see BuildStyles for definitions) ───────────────
        //  0 = default          1 = header
        //  2 = High even        3 = High odd
        //  4 = High bold ctr    5 = High alt bold ctr
        //  6 = Med even         7 = Med odd
        //  8 = Med bold ctr     9 = Med alt bold ctr
        // 10 = Low even        11 = Low odd
        // 12 = Low bold ctr    13 = Low alt bold ctr

        // ══════════════════════════════════════════════════════════════════
        // EXCEL .xlsx  (Open XML via ZipArchive – no external packages)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Writes a colored Open XML .xlsx workbook.
        /// Header row: dark-navy background / white bold text / frozen.
        /// Data rows: risk-based color (red/yellow/green) with alternating
        /// stripe, matching the grid's ApplyRiskColor exactly.
        /// </summary>
        public static void ToXlsx(List<ComponentInfo> components, string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);

            using var stream = File.Create(filePath);
            using var zip    = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);

            AddEntry(zip, "[Content_Types].xml",           ContentTypes());
            AddEntry(zip, "_rels/.rels",                   RootRels());
            AddEntry(zip, "xl/workbook.xml",               Workbook());
            AddEntry(zip, "xl/_rels/workbook.xml.rels",    WorkbookRels());
            AddEntry(zip, "xl/styles.xml",                 BuildStyles());
            AddEntry(zip, "xl/worksheets/sheet1.xml",      BuildSheet(components));
        }

        // ── Package part builders ─────────────────────────────────────────

        private static string ContentTypes() =>
            Xml("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>");

        private static string RootRels() =>
            Xml("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" " +
                  "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" " +
                  "Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");

        private static string Workbook() =>
            Xml("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Component Analysis\" sheetId=\"1\" r:id=\"rId2\"/></sheets>" +
                "</workbook>");

        private static string WorkbookRels() =>
            Xml("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" " +
                  "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" " +
                  "Target=\"styles.xml\"/>" +
                "<Relationship Id=\"rId2\" " +
                  "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" " +
                  "Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>");

        // ── Styles (fonts / fills / borders / cellXfs) ───────────────────

        private static string BuildStyles()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n");
            sb.Append("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

            // Fonts  (8 entries)
            // 0: default   1: header (white bold)
            // 2: high      3: medium     4: low
            // 5: high bold 6: med bold   7: low bold
            sb.Append("<fonts count=\"8\">");
            sb.Append(Font("Segoe UI", 9,  null,        bold: false));  // 0 default
            sb.Append(Font("Segoe UI", 10, "FFFFFFFF",  bold: true));   // 1 header
            sb.Append(Font("Segoe UI", 9,  "FF5C0000",  bold: false));  // 2 high
            sb.Append(Font("Segoe UI", 9,  "FF5C4A00",  bold: false));  // 3 medium
            sb.Append(Font("Segoe UI", 9,  "FF1A4D1A",  bold: false));  // 4 low
            sb.Append(Font("Segoe UI", 9,  "FF5C0000",  bold: true));   // 5 high bold
            sb.Append(Font("Segoe UI", 9,  "FF5C4A00",  bold: true));   // 6 med bold
            sb.Append(Font("Segoe UI", 9,  "FF1A4D1A",  bold: true));   // 7 low bold
            sb.Append("</fonts>");

            // Fills  (9 entries; 0=none and 1=gray125 are required by OOXML)
            // 2: navy header  3: High  4: HighAlt  5: Med  6: MedAlt  7: Low  8: LowAlt
            sb.Append("<fills count=\"9\">");
            sb.Append("<fill><patternFill patternType=\"none\"/></fill>");
            sb.Append("<fill><patternFill patternType=\"gray125\"/></fill>");
            sb.Append(Fill("FF1E3A5F"));  // 2 header navy
            sb.Append(Fill("FFF08080"));  // 3 High even
            sb.Append(Fill("FFD96B6B"));  // 4 High odd (darker)
            sb.Append(Fill("FFF0E68C"));  // 5 Medium even
            sb.Append(Fill("FFD4CA7A"));  // 6 Medium odd (darker)
            sb.Append(Fill("FF90EE90"));  // 7 Low even
            sb.Append(Fill("FF7ED07E"));  // 8 Low odd (darker)
            sb.Append("</fills>");

            // Borders  (2 entries)
            // 0: none   1: thin gray bottom
            sb.Append("<borders count=\"2\">");
            sb.Append("<border><left/><right/><top/><bottom/><diagonal/></border>");
            sb.Append("<border><left/><right/><top/>" +
                      "<bottom style=\"thin\"><color rgb=\"FFCCCCCC\"/></bottom>" +
                      "<diagonal/></border>");
            sb.Append("</borders>");

            // Required base xf
            sb.Append("<cellStyleXfs count=\"1\">");
            sb.Append("<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>");
            sb.Append("</cellStyleXfs>");

            // cellXfs  — 14 entries (indexes 0-13, matching the constants above)
            sb.Append("<cellXfs count=\"14\">");
            //  0 default
            sb.Append(Xf(0, 0, 0, 0, center: false));
            //  1 header
            sb.Append(Xf(1, 2, 0, 0, center: true));
            //  2 High even         3 High odd
            sb.Append(Xf(2, 3, 1, 0)); sb.Append(Xf(2, 4, 1, 0));
            //  4 High bold ctr     5 High alt bold ctr
            sb.Append(Xf(5, 3, 1, 0, center: true)); sb.Append(Xf(5, 4, 1, 0, center: true));
            //  6 Med even          7 Med odd
            sb.Append(Xf(3, 5, 1, 0)); sb.Append(Xf(3, 6, 1, 0));
            //  8 Med bold ctr      9 Med alt bold ctr
            sb.Append(Xf(6, 5, 1, 0, center: true)); sb.Append(Xf(6, 6, 1, 0, center: true));
            // 10 Low even         11 Low odd
            sb.Append(Xf(4, 7, 1, 0)); sb.Append(Xf(4, 8, 1, 0));
            // 12 Low bold ctr     13 Low alt bold ctr
            sb.Append(Xf(7, 7, 1, 0, center: true)); sb.Append(Xf(7, 8, 1, 0, center: true));
            sb.Append("</cellXfs>");

            sb.Append("</styleSheet>");
            return sb.ToString();
        }

        // ── Worksheet ─────────────────────────────────────────────────────

        private static string BuildSheet(List<ComponentInfo> components)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

            // Freeze top row
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\">");
            sb.Append("<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>");
            sb.Append("<selection pane=\"bottomLeft\" activeCell=\"A2\" sqref=\"A2\"/>");
            sb.Append("</sheetView></sheetViews>");

            sb.Append("<sheetFormatPr defaultRowHeight=\"18\"/>");

            // Column widths (in character units)
            int[] widths = { 14, 28, 10, 20, 16, 14, 10, 20, 20, 22, 22, 40 };
            sb.Append("<cols>");
            for (int i = 0; i < widths.Length; i++)
                sb.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{widths[i]}\" customWidth=\"1\"/>");
            sb.Append("</cols>");

            sb.Append("<sheetData>");

            // ── Header row (rowIndex 1) ────────────────────────────────────
            sb.Append("<row r=\"1\" ht=\"22\" customHeight=\"1\">");
            for (int c = 0; c < Headers.Length; c++)
                sb.Append(Cell(1, c + 1, styleIdx: 1, Headers[c]));
            sb.Append("</row>");

            // ── Data rows ─────────────────────────────────────────────────
            int rowIdx = 2;
            foreach (var comp in components)
            {
                bool isAlt = (rowIdx % 2) == 0;   // even rowIdx = alternate shade

                // Base style for all cells in this row
                int baseStyle = (comp.Risk, isAlt) switch
                {
                    (RiskLevel.High,   false) => 2,
                    (RiskLevel.High,   true)  => 3,
                    (RiskLevel.Medium, false) => 6,
                    (RiskLevel.Medium, true)  => 7,
                    (_,                false) => 10,
                    (_,                true)  => 11,
                };

                // Bold + centered style for the Risk column (column 7)
                int riskStyle = (comp.Risk, isAlt) switch
                {
                    (RiskLevel.High,   false) => 4,
                    (RiskLevel.High,   true)  => 5,
                    (RiskLevel.Medium, false) => 8,
                    (RiskLevel.Medium, true)  => 9,
                    (_,                false) => 12,
                    (_,                true)  => 13,
                };

                sb.Append($"<row r=\"{rowIdx}\" ht=\"18\">");
                sb.Append(Cell(rowIdx,  1, baseStyle,  comp.ComponentId.ToString()));
                sb.Append(Cell(rowIdx,  2, baseStyle,  comp.Name));
                sb.Append(Cell(rowIdx,  3, baseStyle,  comp.ComponentType.ToString()));
                sb.Append(Cell(rowIdx,  4, baseStyle,  ComponentNameResolver.TypeLabel(comp.ComponentType)));
                sb.Append(Cell(rowIdx,  5, baseStyle,  comp.Lifecycle.ToString()));
                sb.Append(Cell(rowIdx,  6, baseStyle,  comp.Category.ToString()));
                sb.Append(Cell(rowIdx,  7, riskStyle,  comp.Risk.ToString()));   // bold + centered
                sb.Append(Cell(rowIdx,  8, baseStyle,  comp.SourceVersionDetails  ?? string.Empty));
                sb.Append(Cell(rowIdx,  9, baseStyle,  comp.TargetVersionDetails  ?? string.Empty));
                sb.Append(Cell(rowIdx, 10, baseStyle,  comp.MissingPatches        ?? string.Empty));
                sb.Append(Cell(rowIdx, 11, baseStyle,  comp.LastTargetSolutionName ?? string.Empty));
                sb.Append(Cell(rowIdx, 12, baseStyle,  comp.RiskReason            ?? string.Empty));
                sb.Append("</row>");

                rowIdx++;
            }

            sb.Append("</sheetData></worksheet>");
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════
        // CSV
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Generates a plain CSV string — all columns matching the grid.</summary>
        public static string ToCsv(List<ComponentInfo> components)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ComponentId,Name,TypeCode,TypeLabel,Lifecycle,Category,Risk," +
                          "SourceVersion,TargetVersion,MissingPatches,LastTargetPatch,Note");
            foreach (var c in components)
            {
                sb.AppendLine(
                    $"\"{CsvEsc(c.ComponentId.ToString())}\"," +
                    $"\"{CsvEsc(c.Name)}\"," +
                    $"{c.ComponentType}," +
                    $"\"{CsvEsc(ComponentNameResolver.TypeLabel(c.ComponentType))}\"," +
                    $"{c.Lifecycle},{c.Category},{c.Risk}," +
                    $"\"{CsvEsc(c.SourceVersionDetails  ?? string.Empty)}\"," +
                    $"\"{CsvEsc(c.TargetVersionDetails  ?? string.Empty)}\"," +
                    $"\"{CsvEsc(c.MissingPatches        ?? string.Empty)}\"," +
                    $"\"{CsvEsc(c.LastTargetSolutionName ?? string.Empty)}\"," +
                    $"\"{CsvEsc(c.RiskReason            ?? string.Empty)}\"");
            }
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════
        // PAC CLI script
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Generates a PAC CLI shell script to add components to a solution.</summary>
        public static string ToPacCli(List<ComponentInfo> components, string solutionUniqueName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#!/bin/bash");
            sb.AppendLine($"# PAC CLI – add components to solution: {solutionUniqueName}");
            sb.AppendLine("# Generated by Solution Deployment Advisor");
            sb.AppendLine();
            sb.AppendLine($"pac solution add-component --solution-name \"{solutionUniqueName}\" \\");
            foreach (var c in components)
            {
                sb.AppendLine($"  # {c.Name} ({ComponentNameResolver.TypeLabel(c.ComponentType)})");
                sb.AppendLine($"  # pac solution add-component --solution \"{solutionUniqueName}\" " +
                              $"--component {c.ComponentId} --component-type {c.ComponentType}");
            }
            return sb.ToString();
        }

        public static void SaveToFile(string content, string path)
            => File.WriteAllText(path, content, Encoding.UTF8);

        // ══════════════════════════════════════════════════════════════════
        // Private helpers
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Writes a UTF-8 (no BOM) string as a zip entry.</summary>
        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(content);
        }

        /// <summary>Pass-through – exists so the helpers read naturally.</summary>
        private static string Xml(string s) => s;

        /// <summary>Builds a &lt;font&gt; element.</summary>
        private static string Font(string name, int size, string? rgbColor, bool bold)
        {
            var s = "<font>";
            if (bold)      s += "<b/>";
            s += $"<sz val=\"{size}\"/>";
            if (rgbColor != null) s += $"<color rgb=\"{rgbColor}\"/>";
            s += $"<name val=\"{name}\"/>";
            s += "</font>";
            return s;
        }

        /// <summary>Builds a solid &lt;fill&gt; element.</summary>
        private static string Fill(string rgb) =>
            $"<fill><patternFill patternType=\"solid\">" +
            $"<fgColor rgb=\"{rgb}\"/><bgColor indexed=\"64\"/>" +
            $"</patternFill></fill>";

        /// <summary>Builds a &lt;xf&gt; cell format element.</summary>
        private static string Xf(int fontId, int fillId, int borderId, int xfId, bool center = false)
        {
            string apply = $" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"";
            string align = center
                ? " applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>"
                : "/>";
            return $"<xf numFmtId=\"0\" fontId=\"{fontId}\" fillId=\"{fillId}\" " +
                   $"borderId=\"{borderId}\" xfId=\"{xfId}\"{apply}{align}";
        }

        /// <summary>Converts 1-based column index to Excel letter(s): 1→A, 26→Z, 27→AA…</summary>
        private static string ColLetter(int col)
        {
            string result = string.Empty;
            while (col > 0)
            {
                col--;
                result = (char)('A' + col % 26) + result;
                col /= 26;
            }
            return result;
        }

        /// <summary>Builds an inline-string cell element.</summary>
        private static string Cell(int row, int col, int styleIdx, string value) =>
            $"<c r=\"{ColLetter(col)}{row}\" s=\"{styleIdx}\" t=\"inlineStr\">" +
            $"<is><t>{XmlEsc(value)}</t></is></c>";

        /// <summary>Escapes special XML characters in cell values.</summary>
        private static string XmlEsc(string s) =>
            s.Replace("&", "&amp;")
             .Replace("<", "&lt;")
             .Replace(">", "&gt;")
             .Replace("\"", "&quot;")
             .Replace("\r", " ")
             .Replace("\n", " | ");

        /// <summary>Escapes double-quotes for CSV values.</summary>
        private static string CsvEsc(string s) =>
            s.Replace("\"", "\"\"").Replace("\r", "").Replace("\n", " | ");
    }
}
