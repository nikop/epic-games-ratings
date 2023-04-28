using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater.Markdown
{
    public class MarkdownTable<T>
    {
        List<MarkdownColumn<T>> Columns { get; set; } = new();

        public MarkdownTable<T> AddColumn(string column, Func<T, string> itemFormatter)
        {
            var col = new MarkdownColumn<T>(this, column, itemFormatter);

            Columns.Add(col);

            return this;
        }

        public string FormatTable(IEnumerable<T> items)
        {
            var sb = new StringBuilder();

            sb.Append("| ");
            sb.Append(string.Join(" | ", Columns.Select(x => x.Column)));
            sb.AppendLine(" |");

            sb.Append("| ");
            sb.Append(string.Join(" | ", Columns.Select(x => x.Align)));
            sb.AppendLine(" |");

            foreach (var item in items)
            {
                sb.Append("| ");
                sb.Append(string.Join(" | ", Columns.Select(x => 
                    MarkdownHelpers.Escape(x.ItemFormatter(item))
                )));
                sb.AppendLine(" |");
            }

            return sb.ToString();
        }
    }
}
