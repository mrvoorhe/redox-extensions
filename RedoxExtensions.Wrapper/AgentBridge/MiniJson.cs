using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Minimal JSON reader/writer so the wrapper doesn't take a Newtonsoft dependency, which would get
/// locked in the install directory and could conflict with the copy the wrapped plugin loads.
///
/// Objects parse to Dictionary&lt;string, object&gt;, arrays to List&lt;object&gt;, numbers to double.
/// </summary>
public static class MiniJson
{
    public static object Parse(string json)
    {
        var parser = new Parser(json);
        parser.SkipWhitespace();
        var value = parser.ParseValue();
        parser.SkipWhitespace();
        if (!parser.AtEnd)
            throw new FormatException($"Unexpected trailing characters at {parser.Position}");
        return value;
    }

    public static string Serialize(object value)
    {
        var writer = new StringWriter(CultureInfo.InvariantCulture);
        Serialize(value, writer);
        return writer.ToString();
    }

    public static void Serialize(object value, TextWriter writer)
    {
        switch (value)
        {
            case null:
                writer.Write("null");
                break;
            case string s:
                WriteString(s, writer);
                break;
            case bool b:
                writer.Write(b ? "true" : "false");
                break;
            case char c:
                WriteString(c.ToString(), writer);
                break;
            case double d:
                writer.Write(double.IsNaN(d) || double.IsInfinity(d) ? "null" : d.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float f:
                Serialize((double)f, writer);
                break;
            case decimal m:
                writer.Write(m.ToString(CultureInfo.InvariantCulture));
                break;
            case Enum e:
                WriteString(e.ToString(), writer);
                break;
            case IFormattable formattable when IsIntegral(value):
                writer.Write(formattable.ToString(null, CultureInfo.InvariantCulture));
                break;
            case DateTime dt:
                WriteString(dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), writer);
                break;
            case IDictionary dict:
                writer.Write('{');
                var first = true;
                foreach (DictionaryEntry entry in dict)
                {
                    if (!first)
                        writer.Write(',');
                    first = false;
                    WriteString(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), writer);
                    writer.Write(':');
                    Serialize(entry.Value, writer);
                }
                writer.Write('}');
                break;
            case IEnumerable list:
                writer.Write('[');
                var firstItem = true;
                foreach (var item in list)
                {
                    if (!firstItem)
                        writer.Write(',');
                    firstItem = false;
                    Serialize(item, writer);
                }
                writer.Write(']');
                break;
            default:
                WriteString(value.ToString(), writer);
                break;
        }
    }

    public static string GetString(IDictionary<string, object> obj, string key)
    {
        return obj != null && obj.TryGetValue(key, out var value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
    }

    public static long? GetLong(IDictionary<string, object> obj, string key)
    {
        if (obj == null || !obj.TryGetValue(key, out var value) || value == null)
            return null;
        if (value is double d)
            return (long)d;
        return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public static List<string> GetStringList(IDictionary<string, object> obj, string key)
    {
        if (obj == null || !obj.TryGetValue(key, out var value) || value == null)
            return null;
        var result = new List<string>();
        if (value is List<object> list)
        {
            foreach (var item in list)
                result.Add(Convert.ToString(item, CultureInfo.InvariantCulture));
        }
        else
        {
            result.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
        return result;
    }

    private static bool IsIntegral(object value) =>
        value is int || value is long || value is short || value is byte || value is uint || value is ulong || value is ushort || value is sbyte;

    private static void WriteString(string s, TextWriter writer)
    {
        writer.Write('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': writer.Write("\\\""); break;
                case '\\': writer.Write("\\\\"); break;
                case '\n': writer.Write("\\n"); break;
                case '\r': writer.Write("\\r"); break;
                case '\t': writer.Write("\\t"); break;
                case '\b': writer.Write("\\b"); break;
                case '\f': writer.Write("\\f"); break;
                default:
                    if (c < 0x20)
                        writer.Write("\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        writer.Write(c);
                    break;
            }
        }
        writer.Write('"');
    }

    private class Parser
    {
        private readonly string _json;
        private int _pos;

        public Parser(string json)
        {
            this._json = json ?? throw new ArgumentNullException(nameof(json));
        }

        public int Position => this._pos;

        public bool AtEnd => this._pos >= this._json.Length;

        public void SkipWhitespace()
        {
            while (!this.AtEnd && char.IsWhiteSpace(this._json[this._pos]))
                this._pos++;
        }

        public object ParseValue()
        {
            if (this.AtEnd)
                throw new FormatException("Unexpected end of JSON");

            var c = this._json[this._pos];
            switch (c)
            {
                case '{': return this.ParseObject();
                case '[': return this.ParseArray();
                case '"': return this.ParseString();
                case 't': this.Expect("true"); return true;
                case 'f': this.Expect("false"); return false;
                case 'n': this.Expect("null"); return null;
                default:
                    if (c == '-' || char.IsDigit(c))
                        return this.ParseNumber();
                    throw new FormatException($"Unexpected character '{c}' at {this._pos}");
            }
        }

        private Dictionary<string, object> ParseObject()
        {
            var result = new Dictionary<string, object>();
            this._pos++;
            this.SkipWhitespace();
            if (this.TryConsume('}'))
                return result;

            while (true)
            {
                this.SkipWhitespace();
                if (this.AtEnd || this._json[this._pos] != '"')
                    throw new FormatException($"Expected property name at {this._pos}");
                var key = this.ParseString();
                this.SkipWhitespace();
                if (!this.TryConsume(':'))
                    throw new FormatException($"Expected ':' at {this._pos}");
                this.SkipWhitespace();
                result[key] = this.ParseValue();
                this.SkipWhitespace();
                if (this.TryConsume('}'))
                    return result;
                if (!this.TryConsume(','))
                    throw new FormatException($"Expected ',' or '}}' at {this._pos}");
            }
        }

        private List<object> ParseArray()
        {
            var result = new List<object>();
            this._pos++;
            this.SkipWhitespace();
            if (this.TryConsume(']'))
                return result;

            while (true)
            {
                this.SkipWhitespace();
                result.Add(this.ParseValue());
                this.SkipWhitespace();
                if (this.TryConsume(']'))
                    return result;
                if (!this.TryConsume(','))
                    throw new FormatException($"Expected ',' or ']' at {this._pos}");
            }
        }

        private string ParseString()
        {
            var sb = new StringBuilder();
            this._pos++;
            while (true)
            {
                if (this.AtEnd)
                    throw new FormatException("Unterminated string");
                var c = this._json[this._pos++];
                if (c == '"')
                    return sb.ToString();
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (this.AtEnd)
                    throw new FormatException("Unterminated escape");
                var escaped = this._json[this._pos++];
                switch (escaped)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (this._pos + 4 > this._json.Length)
                            throw new FormatException("Bad unicode escape");
                        sb.Append((char)int.Parse(this._json.Substring(this._pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        this._pos += 4;
                        break;
                    default:
                        throw new FormatException($"Bad escape '\\{escaped}'");
                }
            }
        }

        private double ParseNumber()
        {
            var start = this._pos;
            while (!this.AtEnd && "+-0123456789.eE".IndexOf(this._json[this._pos]) >= 0)
                this._pos++;
            return double.Parse(this._json.Substring(start, this._pos - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private void Expect(string literal)
        {
            if (string.CompareOrdinal(this._json, this._pos, literal, 0, literal.Length) != 0)
                throw new FormatException($"Expected '{literal}' at {this._pos}");
            this._pos += literal.Length;
        }

        private bool TryConsume(char c)
        {
            if (!this.AtEnd && this._json[this._pos] == c)
            {
                this._pos++;
                return true;
            }
            return false;
        }
    }
}
