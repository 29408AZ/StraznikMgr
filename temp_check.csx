using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var filePath = @"C:\Users\zaken\source\repos\Straznik\pdsg.xlsx";
using var package = new ExcelPackage(new FileInfo(filePath));

Console.WriteLine("=== ARKUSZ ZALOGI ===");
var zalogaSheet = package.Workbook.Worksheets["Zalogi"];
if (zalogaSheet != null) {
    var stanowiskaZalogi = new HashSet<string>();
    int col = 1;
    while (!string.IsNullOrWhiteSpace(zalogaSheet.Cells[1, col].Text)) {
        var kat = zalogaSheet.Cells[1, col].Text;
        if (kat.StartsWith("KAT")) {
            int row = 2;
            while (row <= zalogaSheet.Dimension.End.Row) {
                var st = zalogaSheet.Cells[row, col].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(st)) stanowiskaZalogi.Add($"{kat}: {st}");
                row++;
            }
        }
        col++;
    }
    foreach (var s in stanowiskaZalogi.OrderBy(x => x)) Console.WriteLine(s);
}

Console.WriteLine("\n=== ARKUSZ SWIADECTWA (unikalne stanowiska) ===");
var swiadSheet = package.Workbook.Worksheets["Swiadectwa"];
if (swiadSheet != null) {
    var stanowiskaSwiad = new HashSet<string>();
    int row = 2;
    while (!string.IsNullOrWhiteSpace(swiadSheet.Cells[row, 1].Text)) {
        var st = swiadSheet.Cells[row, 3].Text?.Trim();
        var kat = swiadSheet.Cells[row, 4].Text?.Trim();
        if (!string.IsNullOrWhiteSpace(st)) stanowiskaSwiad.Add($"{kat}: {st}");
        row++;
    }
    foreach (var s in stanowiskaSwiad.OrderBy(x => x)) Console.WriteLine(s);
}
