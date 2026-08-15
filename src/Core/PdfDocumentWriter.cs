using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSL_SentinelPro.src.Core
{
    public static class PdfDocumentWriter
    {
        public static async Task WriteAsync(string path, IEnumerable<string> lines, string title)
        {
            var objects = new List<string>();
            var pages = BuildPages(lines.ToList(), title);
            var pageObjectNumbers = new List<int>();

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add("");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            foreach (var page in pages)
            {
                var contentNumber = objects.Count + 2;
                var pageNumber = objects.Count + 1;
                pageObjectNumbers.Add(pageNumber);
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentNumber} 0 R >>");
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(page)} >>\nstream\n{page}\nendstream");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"))}] /Count {pageObjectNumbers.Count} >>";
            await File.WriteAllBytesAsync(path, BuildPdfBytes(objects));
        }

        private static List<string> BuildPages(List<string> lines, string title)
        {
            const int linesPerPage = 43;
            var wrapped = lines.SelectMany(line => WrapLine(line, 92)).ToList();
            var pages = new List<string>();

            for (var index = 0; index < wrapped.Count; index += linesPerPage)
            {
                var chunk = wrapped.Skip(index).Take(linesPerPage).ToList();
                var sb = new StringBuilder();
                sb.AppendLine("BT");
                sb.AppendLine("/F1 16 Tf");
                sb.AppendLine("50 748 Td");
                sb.AppendLine($"({Escape(title)}) Tj");
                sb.AppendLine("/F1 9 Tf");
                sb.AppendLine("0 -24 Td");

                foreach (var line in chunk)
                {
                    sb.AppendLine($"({Escape(line)}) Tj");
                    sb.AppendLine("0 -14 Td");
                }

                sb.AppendLine("ET");
                pages.Add(sb.ToString());
            }

            if (pages.Count == 0)
                pages.Add("BT\n/F1 12 Tf\n50 748 Td\n(Sin informacion disponible.) Tj\nET");

            return pages;
        }

        private static IEnumerable<string> WrapLine(string line, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                yield return string.Empty;
                yield break;
            }

            var words = line.Split(' ');
            var current = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length > maxLength)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }

                    for (var i = 0; i < word.Length; i += maxLength)
                        yield return word.Substring(i, Math.Min(maxLength, word.Length - i));
                    continue;
                }

                if (current.Length + word.Length + 1 > maxLength)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                if (current.Length > 0)
                    current.Append(' ');
                current.Append(word);
            }

            if (current.Length > 0)
                yield return current.ToString();
        }

        private static byte[] BuildPdfBytes(List<string> objects)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, true) { NewLine = "\n" };
            var offsets = new List<long> { 0 };
            writer.Write("%PDF-1.4\n");
            writer.Flush();

            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(ms.Position);
                writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
                writer.Flush();
            }

            var xref = ms.Position;
            writer.Write($"xref\n0 {objects.Count + 1}\n");
            writer.Write("0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
                writer.Write($"{offset:0000000000} 00000 n \n");
            writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            writer.Flush();
            return ms.ToArray();
        }

        private static string Escape(string value)
        {
            var normalized = value
                .Replace("\u00e1", "a").Replace("\u00e9", "e").Replace("\u00ed", "i").Replace("\u00f3", "o").Replace("\u00fa", "u")
                .Replace("\u00c1", "A").Replace("\u00c9", "E").Replace("\u00cd", "I").Replace("\u00d3", "O").Replace("\u00da", "U")
                .Replace("\u00f1", "n").Replace("\u00d1", "N")
                .Replace("\u00fc", "u").Replace("\u00dc", "U");
            return normalized.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
    }
}
