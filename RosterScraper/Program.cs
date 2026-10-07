using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        // Download HTML using HttpClient
        string url = "https://www.parkrun.org.uk/havant/futureroster/";
        
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
            
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"HTML length: {html.Length}");
                File.WriteAllText("../page.html", html);

            // Parse with HtmlAgilityPack
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find the table - adjust selector if needed
            var table = doc.DocumentNode.SelectSingleNode("//table");
            if (table == null)
            {
                Console.WriteLine("Table not found.");
                return;
            }

            // Get rows
            var rows = table.SelectNodes(".//tr");
            if (rows == null || rows.Count == 0)
            {
                Console.WriteLine("No rows found.");
                return;
            }

            // Header row
            var headerRow = rows[0];
            var headerCells = headerRow.SelectNodes(".//th | .//td");
            var dates = headerCells.Skip(1).Select(cell => cell.InnerText.Trim()).ToList();

            // Select next 4 dates after first
            var selectedDates = dates.Skip(1).Take(4).ToList();

            // Data rows
            var data = new List<List<string>>();
            foreach (var row in rows.Skip(1))
            {
                // Get role from the <th> tag
                var roleCell = row.SelectSingleNode(".//th");
                if (roleCell == null) continue;
                var role = roleCell.InnerText.Trim();
                if (role.ToLower() == "pacer") continue;
                
                // Get names from the <td> tags
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count == 0) continue;
                var names = cells.Select(cell => cell.InnerText.Trim()).ToList();
                var selectedNames = names.Skip(1).Take(4).ToList();
                data.Add(new List<string> { role }.Concat(selectedNames).ToList());
            }

            // Generate HTML
            var htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html lang=\"en\">");
            htmlBuilder.AppendLine("<head>");
            htmlBuilder.AppendLine("    <meta charset=\"UTF-8\">");
            htmlBuilder.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBuilder.AppendLine("    <title>Future Roster</title>");
            htmlBuilder.AppendLine("    <style>");
            htmlBuilder.AppendLine("        @page {");
            htmlBuilder.AppendLine("            size: A4 portrait;");
            htmlBuilder.AppendLine("            margin: 5mm;");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine("        body {");
            htmlBuilder.AppendLine("            margin: 0;");
            htmlBuilder.AppendLine("            padding: 0;");
            htmlBuilder.AppendLine("            font-family: Arial, sans-serif;");
            htmlBuilder.AppendLine("            font-size: 12px;");
            htmlBuilder.AppendLine("            height: 100%;");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine("        html {");
            htmlBuilder.AppendLine("            height: 100%;");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine("        table {");
            htmlBuilder.AppendLine("            width: 100%;");
            htmlBuilder.AppendLine("            height: 100%;");
            htmlBuilder.AppendLine("            border-collapse: collapse;");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine("        thead {");
            htmlBuilder.AppendLine("            display: table-header-group;");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine($"        tbody tr {{");
            htmlBuilder.AppendLine($"            height: calc(100% / {data.Count});");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine("        th, td {");
            htmlBuilder.AppendLine("            border: 1px solid black;");
            htmlBuilder.AppendLine("            padding: 5px;");
            htmlBuilder.AppendLine("            text-align: center;");
            htmlBuilder.AppendLine("            vertical-align: middle;");
            htmlBuilder.AppendLine("            width: 20%;");
            htmlBuilder.AppendLine("        }");
            htmlBuilder.AppendLine("        th {");
            htmlBuilder.AppendLine("            background-color: #f0f0f0;");
            htmlBuilder.AppendLine("        }");;
            htmlBuilder.AppendLine("    </style>");
            htmlBuilder.AppendLine("</head>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("    <table>");
            htmlBuilder.AppendLine("        <tr>");
            htmlBuilder.AppendLine("            <th>Role</th>");
            foreach (var date in selectedDates)
            {
                htmlBuilder.AppendLine($"            <th>{WebUtility.HtmlEncode(date)}</th>");
            }
            htmlBuilder.AppendLine("        </tr>");
            foreach (var row in data)
            {
                htmlBuilder.AppendLine("        <tr>");
                foreach (var cell in row)
                {
                    htmlBuilder.AppendLine($"            <td>{WebUtility.HtmlEncode(cell)}</td>");
                }
                htmlBuilder.AppendLine("        </tr>");
            }
            htmlBuilder.AppendLine("    </table>");
            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            // Write to file
            string outputPath = Path.GetFullPath("../roster.html");
            File.WriteAllText(outputPath, htmlBuilder.ToString());
            Console.WriteLine("Roster HTML generated: " + outputPath);
            
            // Open the file in the default browser
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = outputPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error downloading page: {e.Message}");
            }
        }
    }
}
