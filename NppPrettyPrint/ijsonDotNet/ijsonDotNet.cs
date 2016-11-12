using System;
//using System.Collections;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace ijsonDotNet
{
    public class ijsonParser
    {
        const int BUFSIZE = 16 * 1024;
        private Regex LEXEME_RE { get; set; }
        private ijsonCommon common { get; set; }

        public ijsonParser()
        {
            LEXEME_RE = new Regex(@"[a-z0-9eE\.\+-]+|\S");
            common = new ijsonCommon();
        }

        public IEnumerable<ijsonLexerEvent> lexer(TextReader f, int bufsize = BUFSIZE)
        {
            char[] buf = new char[BUFSIZE];
            int pos = 0;
            int discarded = 0;
            int start, end, escpos;
            int read = f.Read(buf, 0, BUFSIZE);
            string buff = new string(buf, 0, read);
            string lexeme;

            while (true)
            {
                var match = LEXEME_RE.Match(buff, pos);
                if (match.Success)
                {
                    lexeme = match.Value;
                    if (lexeme == "\"")
                    {
                        pos = match.Index;
                        start = pos + 1;
                        while (true)
                        {
                            try
                            {
                                end = buff.IndexOf('"', start);
                                escpos = end - 1;
                                while (buff[escpos] == '\\')
                                    escpos -= 1;

                                if ((end - escpos) % 2 == 0)
                                    start = end + 1;
                                else
                                    break;
                            }
                            catch (Exception)
                            {
                                read = f.Read(buf, 0, BUFSIZE);
                                if (read == 0)
                                    throw new IncompleteJSONError("Incomplete string lexeme");

                                buff += new string(buf, 0, read);
                            }
                        }

                        yield return new ijsonLexerEvent { pos = discarded + pos, symbol = buff.Substring(pos, end + 1 - pos) };
                        pos = end + 1;
                    }
                    else
                    {
                        while ((match.Index + match.Length) == buff.Length)
                        {
                            read = f.Read(buf, 0, BUFSIZE);
                            if (read == 0)
                                break;

                            buff += new string(buf, 0, read);
                            match = LEXEME_RE.Match(buff, pos);
                            lexeme = match.Value;
                        }

                        yield return new ijsonLexerEvent { pos = discarded + match.Index, symbol = lexeme };
                        pos = match.Index + match.Length;
                    }
                }
                else
                {
                    read = f.Read(buf, 0, BUFSIZE);
                    if (read == 0)
                        break;

                    discarded += buff.Length;
                    buff = new string(buf, 0, read);
                    pos = 0;
                }
            }
        }

        public IEnumerable<ijsonEvent2> parse_value(IEnumerator<ijsonLexerEvent> lexer, object symbol = null, int pos = 0)
        {
            if (symbol == null)
            {
                if (!lexer.MoveNext())
                    throw new IncompleteJSONError("Incomplete JSON data");

                ijsonLexerEvent evt = lexer.Current;
                pos = evt.pos;
                symbol = evt.symbol;
            }

            var sym = symbol.ToString();
            if (sym == "null")
            {
                yield return new ijsonEvent2 { type = ijsonTokenType.Null, value = null };
            }
            else if (sym == "true")
            {
                yield return new ijsonEvent2 { type = ijsonTokenType.Boolean, value = true };
            }
            else if (sym == "false")
            {
                yield return new ijsonEvent2 { type = ijsonTokenType.Boolean, value = false };
            }
            else if (sym == "[")
            {
                foreach (var evnt in parse_array(lexer))
                    yield return evnt;
            }
            else if (sym == "{")
            {
                foreach (var evnt in parse_object(lexer))
                    yield return evnt;
            }
            else if (sym[0] == '"')
            {
                yield return new ijsonEvent2 { type = ijsonTokenType.String, value = sym.Substring(1, sym.Length - 2) };
            }
            else
            {
                double n;
                if (!double.TryParse(sym, out n))
                {
                    throw new UnexpectedSymbol(sym, pos);
                }
                else
                {
                    yield return new ijsonEvent2 { type = ijsonTokenType.Number, value = sym };
                }
            }
        }

        public IEnumerable<ijsonEvent2> parse_array(IEnumerator<ijsonLexerEvent> lexer)
        {
            yield return new ijsonEvent2 { type = ijsonTokenType.StartArray, value = null };
            if (!lexer.MoveNext())
                throw new IncompleteJSONError("Incomplete JSON data");
            
            object symbol = lexer.Current.symbol;
            int pos = lexer.Current.pos;
            string sym = symbol.ToString();
            if (sym != "]")
            {
                while (true)
                {
                    foreach (var evnt in parse_value(lexer, symbol, pos))
                        yield return evnt;

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");
                    
                    symbol = lexer.Current.symbol;
                    pos = lexer.Current.pos;
                    sym = symbol.ToString();

                    if (sym == "]")
                        break;

                    if (sym != ",")
                        throw new UnexpectedSymbol(sym, pos);

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");
                    
                    symbol = lexer.Current.symbol;
                    pos = lexer.Current.pos;
                }
            }
            
            yield return new ijsonEvent2 { type = ijsonTokenType.EndArray, value = null };
        }

        public IEnumerable<ijsonEvent2> parse_object(IEnumerator<ijsonLexerEvent> lexer)
        {
            yield return new ijsonEvent2 { type = ijsonTokenType.StartMap, value = null };
            if (!lexer.MoveNext())
                throw new IncompleteJSONError("Incomplete JSON data");
            
            object symbol = lexer.Current.symbol;
            int pos = lexer.Current.pos;
            string sym = symbol.ToString();
            if (sym != "}")
            {
                while (true)
                {
                    if (sym[0] != '"')
                        throw new UnexpectedSymbol(sym, pos);

                    yield return new ijsonEvent2 { type = ijsonTokenType.MapKey, value = sym.Substring(1, sym.Length - 2) };

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");

                    symbol = lexer.Current.symbol;
                    pos = lexer.Current.pos;
                    sym = symbol.ToString();

                    if (sym != ":")
                        throw new UnexpectedSymbol(sym, pos);

                    foreach (var evnt in parse_value(lexer, null, pos))
                        yield return evnt;

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");

                    symbol = lexer.Current.symbol;
                    pos = lexer.Current.pos;
                    sym = symbol.ToString();

                    if (sym == "}")
                        break;

                    if (sym != ",")
                        throw new UnexpectedSymbol(sym, pos);

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");

                    symbol = lexer.Current.symbol;
                    pos = lexer.Current.pos;
                    sym = symbol.ToString();
                }
            }
            
            yield return new ijsonEvent2 { type = ijsonTokenType.EndMap, value = null };
        }

        public IEnumerable<ijsonEvent2> basic_parse(StreamReader f, int bufsize = BUFSIZE)
        {
            using (var iter = lexer(f, bufsize).GetEnumerator())
            {
                foreach (var value in parse_value(iter))
                    yield return value;

                if (iter.MoveNext())
                    throw new JSONError("Additional data");
            }
        }

        public IEnumerable<ijsonEvent> parse(StreamReader f, int bufsize = BUFSIZE)
        {
            return common.parse(basic_parse(f, bufsize));
        }

        public string pretty(StreamReader f, string indent_string = "\t", string eol_string = "\r\n", int bufsize = BUFSIZE)
        {
            return common.pretty(parse(f, bufsize), indent_string, eol_string);
        }

        public string minify(StreamReader f, int bufsize = BUFSIZE)
        {
            return common.minify(parse(f, bufsize));
        }
    }
}
