
using System.Drawing;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

class EmployeeEntry
{
    public string Id { get; set; }
    public string EmployeeName { get; set; }
    public string StarTimeUtc { get; set; }
    public string EndTimeUtc { get; set; }
    public string EntryNotes { get; set; }
    public string DeletedOn { get; set; }

}

class Program
{
    static async Task Main()
    {
        string apiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

        Console.WriteLine("Fetching data from API.......");
        var http = new HttpClient();
        var data = await http.GetStringAsync(apiUrl);

        Console.WriteLine("Parsing JSON........");
        var option = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var entries = JsonSerializer.Deserialize<List<EmployeeEntry>>(data, option);
        Console.WriteLine("Calculating total hours per employee.....");

        var totals = entries.GroupBy(e => e.EmployeeName).Select(g => new
                {
                    Name = string.IsNullOrEmpty(g.Key) ? "NULL" : g.Key,
                    TotalHours = g.Sum(e =>
        {
            DateTime start = DateTime.Parse(e.StarTimeUtc);
            DateTime end = DateTime.Parse(e.EndTimeUtc);
            return (end - start).TotalHours;
        })
        }).OrderByDescending(x => x.TotalHours).ToList();

        string htmlPath = "Employees.html";
        GenerateHtml(totals, htmlPath);
        Console.WriteLine($"HTML table saved at {Path.GetFullPath(htmlPath)}");

        string chartPath = "PieChart.png";
        GeneratPieChart(totals, chartPath);
        Console.WriteLine($"Pie chart saved at {Path.GetFullPath(chartPath)}");

        Console.WriteLine("Done!");
    }

    static void GenerateHtml(IEnumerable<dynamic> totals, string fileName)
    {
        using (var sw = new StreamWriter(fileName))
        {
            sw.WriteLine("<html><head><style>");
            sw.WriteLine("table {border-collapse: collapse; width: 60%; font-family: Arial; text-align: center; margin-right: auto; margin-left: auto;}");
            sw.WriteLine("th, td{border: 1px solid #999; padding: 8px; text-align: center;}");
            sw.WriteLine(".low {background-color: #eb0606ff;}");
            sw.WriteLine("</style></head><body>");
            sw.WriteLine("<h2 style='text-align: center;'>Employee Total Time Worked Hours</h2>");
            sw.WriteLine("<table>");
            sw.WriteLine("<tr><th>Rank</th><th>Name</th><th>Total Hours</th></tr>");
            int rank = 1;
            foreach (var emp in totals)
            {
                string rowClass = emp.TotalHours < 100 ? " class='low'" : "";
                sw.WriteLine($"<tr{rowClass}><td>{rank}</td><td>{emp.Name}</td><td>{emp.TotalHours:F2}</td></tr>");
                rank++;
            }
            sw.WriteLine("</table>");
            sw.WriteLine("<p style='text-align: center;'>Rows in red worked less than 100 hours.</p>");
            sw.WriteLine("</body></html>");
        }
    }

    static void GeneratPieChart(IEnumerable<dynamic> totals, string fileName)
    {
        int width = 600, height = 400;
        using var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var totalHours = totals.Sum(t => (double)t.TotalHours);
        var rect = new Rectangle(50, 50, 300, 300);
        var colors = new[] { Color.Red, Color.Green, Color.Blue, Color.Orange, Color.Purple, Color.Cyan, Color.Magenta };

        float startAngle = 0;
        int i = 0;
        foreach (var emp in totals)
        {
            float sweep = (float)((emp.TotalHours / totalHours) * 360);
            var color = colors[i % colors.Length];
            using var brush = new SolidBrush(color);

            g.FillPie(brush, rect, startAngle, sweep);
            g.DrawPie(Pens.Black, rect, startAngle, sweep);

            // Legend
            g.FillRectangle(brush, 380, 60 + i * 25, 20, 20);
            g.DrawString($"{emp.Name} - {emp.TotalHours:F1} h", new Font("Arial", 10), Brushes.Black, 410, 60 + i * 25);

            startAngle += sweep;
            i++;
        }

        bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
    }
}
