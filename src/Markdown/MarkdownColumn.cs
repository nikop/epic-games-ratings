using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater.Markdown
{
    public class MarkdownColumn<T>
    {
        public MarkdownTable<T> Table { get; }

        public string Column { get; }

        public Func<T, string> ItemFormatter { get; }

        public string Align => new('-', Column.Length);

        public MarkdownColumn(MarkdownTable<T> table, string column, Func<T, string> itemFormatter)
        {
            Table = table;
            Column = column;
            ItemFormatter = itemFormatter;
        }
    }
}
