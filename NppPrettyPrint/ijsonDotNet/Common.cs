using System.Collections.Generic;
using System.Text;

namespace ijsonDotNet
{
    public class ijsonCommon
    {
        public IEnumerable<ijsonEvent> Parse(IEnumerable<ijsonEvent2> events)
        {
            var path = new List<string>();
            string prefix;
            foreach (var evt in events)
            {
                if (evt.Type == ijsonTokenType.MapKey)
                {
                    prefix = (path.Count > 1) ? string.Join(".", path.GetRange(0, path.Count - 1)) : "";
                    path[path.Count - 1] = evt.Value.ToString();
                }
                else if (evt.Type == ijsonTokenType.StartMap)
                {
                    prefix = string.Join(".", path);
                    path.Add(null);
                }
                else if (evt.Type == ijsonTokenType.EndMap)
                {
                    if (path.Count > 0)
                        path.RemoveAt(path.Count - 1);

                    prefix = string.Join(".", path);
                }
                else if (evt.Type == ijsonTokenType.StartArray)
                {
                    prefix = string.Join(".", path);
                    path.Add("item");
                }
                else if (evt.Type == ijsonTokenType.EndArray)
                {
                    if (path.Count > 0)
                        path.RemoveAt(path.Count - 1);

                    prefix = string.Join(".", path);
                }
                else
                {
                    prefix = string.Join(".", path);
                }

                yield return new ijsonEvent { Prefix = prefix, Type = evt.Type, Value = evt.Value };
            }
        }

        public string Pretty(IEnumerable<ijsonEvent2> events, string indent_string = "\t", string eol_string = "\r\n")
        {
            string indent = "";
            int lenIndent = indent_string.Length;
            int lenEol = eol_string.Length;
            ijsonTokenType prevType = ijsonTokenType.None;
            StringBuilder sOut = new StringBuilder();
            foreach (var evt in events)
            {
                if (evt.Type == ijsonTokenType.StartMap)
                {
                    if (prevType == ijsonTokenType.EndMap || prevType == ijsonTokenType.EndArray)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append(" ");
                    }

                    sOut.Append("{" + eol_string);
                    indent += indent_string;
                }
                else if (evt.Type == ijsonTokenType.EndMap)
                {
                    if (prevType == ijsonTokenType.StartMap)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append("}," + eol_string);
                        indent = indent.Remove(indent.Length - lenIndent);
                    }
                    else
                    {
                        sOut.Remove(sOut.Length - (lenEol + 1), (lenEol + 1));
                        sOut.Append(eol_string);
                        indent = indent.Remove(indent.Length - lenIndent);
                        sOut.Append(indent + "}," + eol_string);
                    }
                }
                else if (evt.Type == ijsonTokenType.StartArray)
                {
                    if (prevType == ijsonTokenType.EndMap || prevType == ijsonTokenType.EndArray)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append(" ");
                    }

                    sOut.Append("[");
                    indent += indent_string;
                }
                else if (evt.Type == ijsonTokenType.EndArray)
                {
                    if (prevType == ijsonTokenType.StartArray)
                    {
                        sOut.Append("]," + eol_string);
                        indent = indent.Remove(indent.Length - lenIndent);
                    }
                    else if (prevType == ijsonTokenType.EndMap)
                    {
                        sOut.Remove(sOut.Length - (lenEol + 1), (lenEol + 1));
                        if (sOut.ToString(sOut.Length - 2, 1) == "{")
                        {
                            sOut.Append("]," + eol_string);
                            indent = indent.Remove(indent.Length - lenIndent);
                        }
                        else
                        {
                            sOut.Append(eol_string);
                            indent = indent.Remove(indent.Length - lenIndent);
                            sOut.Append(indent + "]," + eol_string);
                        }
                    }
                    else
                    {
                        int toRemove = 1;
                        if (sOut.ToString(sOut.Length - lenEol, lenEol) == eol_string)
                            toRemove += lenEol;
                        else
                            toRemove += 1;

                        sOut.Remove(sOut.Length - toRemove, toRemove);
                        sOut.Append("]," + eol_string);
                        indent = indent.Remove(indent.Length - lenIndent);
                    }
                }
                else if (evt.Type == ijsonTokenType.MapKey)
                {
                    sOut.Append(indent + "\"" + evt.Value.ToString() + "\": ");
                }
                else
                {
                    if (prevType == ijsonTokenType.EndMap || prevType == ijsonTokenType.EndArray)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append(" ");
                    }

                    if (evt.Type == ijsonTokenType.Null)
                    {
                        sOut.Append("null,");
                    }
                    else if (evt.Type == ijsonTokenType.Boolean)
                    {
                        sOut.Append(evt.Value.ToString().ToLower() + ",");
                    }
                    else if (evt.Type == ijsonTokenType.Number)
                    {
                        sOut.Append(evt.Value.ToString() + ",");
                    }
                    else if (evt.Type == ijsonTokenType.String)
                    {
                        sOut.Append("\"" + evt.Value.ToString() + "\",");
                    }

                    if (prevType == ijsonTokenType.MapKey)
                        sOut.Append(eol_string);
                    else
                        sOut.Append(" ");
                }

                prevType = evt.Type;
            }

            sOut.Remove(sOut.Length - (lenEol + 1), (lenEol + 1));
            return sOut.ToString();
        }

        public string Minify(IEnumerable<ijsonEvent2> events)
        {
            ijsonTokenType prevType = ijsonTokenType.None;
            StringBuilder sOut = new StringBuilder();
            foreach (var evt in events)
            {
                if (evt.Type == ijsonTokenType.StartMap)
                {
                    sOut.Append("{");
                }
                else if (evt.Type == ijsonTokenType.EndMap)
                {
                    if (prevType != ijsonTokenType.StartMap)
                        sOut.Remove(sOut.Length - 1, 1);

                    sOut.Append("},");
                }
                else if (evt.Type == ijsonTokenType.StartArray)
                {
                    sOut.Append("[");
                }
                else if (evt.Type == ijsonTokenType.EndArray)
                {
                    if (prevType != ijsonTokenType.StartArray)
                        sOut.Remove(sOut.Length - 1, 1);

                    sOut.Append("],");
                }
                else if (evt.Type == ijsonTokenType.MapKey)
                {
                    sOut.Append("\"" + evt.Value.ToString() + "\":");
                }
                else
                {
                    if (evt.Type == ijsonTokenType.Null)
                        sOut.Append("null,");
                    else if (evt.Type == ijsonTokenType.Boolean)
                        sOut.Append(evt.Value.ToString().ToLower() + ",");
                    else if (evt.Type == ijsonTokenType.Number)
                        sOut.Append(evt.Value.ToString() + ",");
                    else if (evt.Type == ijsonTokenType.String)
                        sOut.Append("\"" + evt.Value.ToString() + "\",");
                }

                prevType = evt.Type;
            }

            sOut.Remove(sOut.Length - 1, 1);
            return sOut.ToString();
        }
    }
}