using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace RedoxExtensions.Recording;

/// <summary>Writes JSON the way Python's json.dumps(ensure_ascii=False) does, so the lines look like metachain.py's.</summary>
internal static class RecorderJson
{
    public static string Serialize(object value)
    {
        var sb = new StringBuilder();
        Write(value, sb);
        return sb.ToString();
    }

    private static void Write(object value, StringBuilder sb)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case string s:
                WriteString(s, sb);
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case char c:
                WriteString(c.ToString(), sb);
                break;
            case double d:
                sb.Append(double.IsNaN(d) || double.IsInfinity(d) ? "null" : d.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float f:
                Write((double)f, sb);
                break;
            case decimal m:
                sb.Append(m.ToString(CultureInfo.InvariantCulture));
                break;
            case Enum e:
                WriteString(e.ToString(), sb);
                break;
            case DateTime dt:
                WriteString(dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), sb);
                break;
            case sbyte or byte or short or ushort or int or uint or long or ulong:
                sb.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
                break;
            case IDictionary dict:
                sb.Append('{');
                var first = true;
                foreach (DictionaryEntry entry in dict)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    WriteString(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), sb);
                    sb.Append(": ");
                    Write(entry.Value, sb);
                }
                sb.Append('}');
                break;
            case IEnumerable list:
                sb.Append('[');
                var firstItem = true;
                foreach (var item in list)
                {
                    if (!firstItem)
                        sb.Append(", ");
                    firstItem = false;
                    Write(item, sb);
                }
                sb.Append(']');
                break;
            default:
                WriteString(value.ToString(), sb);
                break;
        }
    }

    private static void WriteString(string s, StringBuilder sb)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}
