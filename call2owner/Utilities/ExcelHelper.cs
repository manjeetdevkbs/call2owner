using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class ExcelHelper
    {
        public static async Task<List<T>> ParseExcelAsync<T>(IFormFile file) where T : new()
        {
            var result = new List<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) throw new Exception("Worksheet not found.");

            var columnMapping = new Dictionary<string, int>(); // column name to index

            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var columnName = worksheet.Cells[1, col].Text.Trim();
                if (!string.IsNullOrWhiteSpace(columnName))
                    columnMapping[columnName] = col;
            }

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var obj = new T();

                foreach (var prop in properties)
                {
                    if (!columnMapping.ContainsKey(prop.Name)) continue;

                    var colIndex = columnMapping[prop.Name];
                    var cellValue = worksheet.Cells[row, colIndex].Text;

                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    try
                    {
                        var safeValue = Convert.ChangeType(cellValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                        prop.SetValue(obj, safeValue);
                    }
                    catch
                    {
                        // Handle parse errors if necessary
                    }
                }

                result.Add(obj);
            }

            return result;
        }
    }
}
