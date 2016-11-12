//using System;
//using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
//using System.Text.RegularExpressions;
//using System.IO;

namespace ijsonDotNet
{
    public class ijsonCommon
    {
        public IEnumerable<ijsonEvent> parse(IEnumerable<ijsonEvent2> events)
        {
            var path = new List<string>();
            string prefix;
            foreach (var evt in events)
            {
                if (evt.type == ijsonTokenType.MapKey)
                {
                    prefix = (path.Count > 1) ? string.Join(".", path.GetRange(0, path.Count - 1)) : "";
                    path[path.Count - 1] = evt.value.ToString();
                }
                else if (evt.type == ijsonTokenType.StartMap)
                {
                    prefix = string.Join(".", path);
                    path.Add(null);
                }
                else if (evt.type == ijsonTokenType.EndMap)
                {
                    if (path.Count > 0)
                        path.RemoveAt(path.Count - 1);

                    prefix = string.Join(".", path);
                }
                else if (evt.type == ijsonTokenType.StartArray)
                {
                    prefix = string.Join(".", path);
                    path.Add("item");
                }
                else if (evt.type == ijsonTokenType.EndArray)
                {
                    if (path.Count > 0)
                        path.RemoveAt(path.Count - 1);

                    prefix = string.Join(".", path);
                }
                else
                {
                    prefix = string.Join(".", path);
                }

                yield return new ijsonEvent { prefix = prefix, type = evt.type, value = evt.value };
            }
        }

        public string pretty(IEnumerable<ijsonEvent> events, string indent_string = "\t", string eol_string = "\r\n")
        {
            string indent = "";
            int lenIndent = indent_string.Length;
            int lenEol = eol_string.Length;
            ijsonTokenType prevType = ijsonTokenType.None;
            StringBuilder sOut = new StringBuilder();
            foreach (var evt in events)
            {
                if (evt.type == ijsonTokenType.StartMap)
                {
                    if (prevType == ijsonTokenType.EndMap || prevType == ijsonTokenType.EndArray)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append(" ");
                    }

                    sOut.Append("{" + eol_string);
                    indent += indent_string;
                }
                else if (evt.type == ijsonTokenType.EndMap)
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
                else if (evt.type == ijsonTokenType.StartArray)
                {
                    if (prevType == ijsonTokenType.EndMap || prevType == ijsonTokenType.EndArray)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append(" ");
                    }

                    sOut.Append("[");
                    indent += indent_string;
                }
                else if (evt.type == ijsonTokenType.EndArray)
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
                else if (evt.type == ijsonTokenType.MapKey)
                {
                    sOut.Append(indent + "\"" + evt.value.ToString() + "\": ");
                }
                else
                {
                    if (prevType == ijsonTokenType.EndMap || prevType == ijsonTokenType.EndArray)
                    {
                        sOut.Remove(sOut.Length - lenEol, lenEol);
                        sOut.Append(" ");
                    }

                    if (evt.type == ijsonTokenType.Null)
                    {
                        sOut.Append("null,");
                    }
                    else if (evt.type == ijsonTokenType.Boolean)
                    {
                        sOut.Append(evt.value.ToString().ToLower() + ",");
                    }
                    else if (evt.type == ijsonTokenType.Number)
                    {
                        sOut.Append(evt.value.ToString() + ",");
                    }
                    else if (evt.type == ijsonTokenType.String)
                    {
                        sOut.Append("\"" + evt.value.ToString() + "\",");
                    }

                    if (prevType == ijsonTokenType.MapKey)
                        sOut.Append(eol_string);
                    else
                        sOut.Append(" ");
                }

                prevType = evt.type;
            }

            sOut.Remove(sOut.Length - (lenEol + 1), (lenEol + 1));
            return sOut.ToString();
        }

        public string minify(IEnumerable<ijsonEvent> events)
        {
            ijsonTokenType prevType = ijsonTokenType.None;
            StringBuilder sOut = new StringBuilder();
            foreach (var evt in events)
            {
                if (evt.type == ijsonTokenType.StartMap)
                {
                    sOut.Append("{");
                }
                else if (evt.type == ijsonTokenType.EndMap)
                {
                    if (prevType != ijsonTokenType.StartMap)
                        sOut.Remove(sOut.Length - 1, 1);

                    sOut.Append("},");
                }
                else if (evt.type == ijsonTokenType.StartArray)
                {
                    sOut.Append("[");
                }
                else if (evt.type == ijsonTokenType.EndArray)
                {
                    if (prevType != ijsonTokenType.StartArray)
                        sOut.Remove(sOut.Length - 1, 1);

                    sOut.Append("],");
                }
                else if (evt.type == ijsonTokenType.MapKey)
                {
                    sOut.Append("\"" + evt.value.ToString() + "\":");
                }
                else
                {
                    if (evt.type == ijsonTokenType.Null)
                        sOut.Append("null,");
                    else if (evt.type == ijsonTokenType.Boolean)
                        sOut.Append(evt.value.ToString().ToLower() + ",");
                    else if (evt.type == ijsonTokenType.Number)
                        sOut.Append(evt.value.ToString() + ",");
                    else if (evt.type == ijsonTokenType.String)
                        sOut.Append("\"" + evt.value.ToString() + "\",");
                }

                prevType = evt.type;
            }

            sOut.Remove(sOut.Length - 1, 1);
            return sOut.ToString();
        }
    }
}