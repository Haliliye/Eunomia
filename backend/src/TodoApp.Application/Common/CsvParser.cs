namespace TodoApp.Application.Common;

/// <summary>
/// Minimal RFC 4180-ish CSV parser — handles quoted fields (including
/// embedded commas/newlines/escaped quotes). No external dependency needed
/// for a handful of simple columns.
/// </summary>
public static class CsvParser
{
    public static List<string[]> Parse(string content)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;

        void EndField() { fields.Add(field.ToString()); field.Clear(); }
        void EndRow() { EndField(); rows.Add(fields.ToArray()); fields = new List<string>(); }

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    EndField();
                    break;
                case '\r':
                    break; // swallow — \n (below) ends the row
                case '\n':
                    EndRow();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        // Final row if the file doesn't end with a newline.
        if (field.Length > 0 || fields.Count > 0) EndRow();

        return rows.Where(r => r.Length > 1 || !string.IsNullOrWhiteSpace(r[0])).ToList();
    }
}
