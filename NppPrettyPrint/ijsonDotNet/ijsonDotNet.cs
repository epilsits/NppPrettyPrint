using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ijsonDotNet
{
    public class ijsonParser
    {
        const int BUFSIZE = 16 * 1024;
        private Regex LEXEME_RE { get; set; }
        private ijsonCommon Common { get; set; }

        public ijsonParser()
        {
            LEXEME_RE = new Regex(@"[a-z0-9eE\.\+-]+|\S");
            Common = new ijsonCommon();
        }

        public IEnumerable<ijsonLexerEvent> Lexer(TextReader f, int bufsize = BUFSIZE)
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

                        yield return new ijsonLexerEvent { Pos = discarded + pos, Symbol = buff.Substring(pos, end + 1 - pos) };
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

                        yield return new ijsonLexerEvent { Pos = discarded + match.Index, Symbol = lexeme };
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

        public IEnumerable<ijsonEvent2> ParseValue(IEnumerator<ijsonLexerEvent> lexer, object symbol = null, int pos = 0)
        {
            if (symbol == null)
            {
                if (!lexer.MoveNext())
                    throw new IncompleteJSONError("Incomplete JSON data");

                ijsonLexerEvent evt = lexer.Current;
                pos = evt.Pos;
                symbol = evt.Symbol;
            }

            var sym = symbol.ToString();
            if (sym == "null")
            {
                yield return new ijsonEvent2 { Type = ijsonTokenType.Null, Value = null };
            }
            else if (sym == "true")
            {
                yield return new ijsonEvent2 { Type = ijsonTokenType.Boolean, Value = true };
            }
            else if (sym == "false")
            {
                yield return new ijsonEvent2 { Type = ijsonTokenType.Boolean, Value = false };
            }
            else if (sym == "[")
            {
                foreach (var evnt in ParseArray(lexer))
                    yield return evnt;
            }
            else if (sym == "{")
            {
                foreach (var evnt in ParseObject(lexer))
                    yield return evnt;
            }
            else if (sym[0] == '"')
            {
                yield return new ijsonEvent2 { Type = ijsonTokenType.String, Value = sym.Substring(1, sym.Length - 2) };
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
                    yield return new ijsonEvent2 { Type = ijsonTokenType.Number, Value = sym };
                }
            }
        }

        public IEnumerable<ijsonEvent2> ParseArray(IEnumerator<ijsonLexerEvent> lexer)
        {
            yield return new ijsonEvent2 { Type = ijsonTokenType.StartArray, Value = null };
            if (!lexer.MoveNext())
                throw new IncompleteJSONError("Incomplete JSON data");
            
            object symbol = lexer.Current.Symbol;
            int pos = lexer.Current.Pos;
            string sym = symbol.ToString();
            if (sym != "]")
            {
                while (true)
                {
                    foreach (var evnt in ParseValue(lexer, symbol, pos))
                        yield return evnt;

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");
                    
                    symbol = lexer.Current.Symbol;
                    pos = lexer.Current.Pos;
                    sym = symbol.ToString();

                    if (sym == "]")
                        break;

                    if (sym != ",")
                        throw new UnexpectedSymbol(sym, pos);

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");
                    
                    symbol = lexer.Current.Symbol;
                    pos = lexer.Current.Pos;
                }
            }
            
            yield return new ijsonEvent2 { Type = ijsonTokenType.EndArray, Value = null };
        }

        public IEnumerable<ijsonEvent2> ParseObject(IEnumerator<ijsonLexerEvent> lexer)
        {
            yield return new ijsonEvent2 { Type = ijsonTokenType.StartMap, Value = null };
            if (!lexer.MoveNext())
                throw new IncompleteJSONError("Incomplete JSON data");
            
            object symbol = lexer.Current.Symbol;
            int pos = lexer.Current.Pos;
            string sym = symbol.ToString();
            if (sym != "}")
            {
                while (true)
                {
                    if (sym[0] != '"')
                        throw new UnexpectedSymbol(sym, pos);

                    yield return new ijsonEvent2 { Type = ijsonTokenType.MapKey, Value = sym.Substring(1, sym.Length - 2) };

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");

                    symbol = lexer.Current.Symbol;
                    pos = lexer.Current.Pos;
                    sym = symbol.ToString();

                    if (sym != ":")
                        throw new UnexpectedSymbol(sym, pos);

                    foreach (var evnt in ParseValue(lexer, null, pos))
                        yield return evnt;

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");

                    symbol = lexer.Current.Symbol;
                    pos = lexer.Current.Pos;
                    sym = symbol.ToString();

                    if (sym == "}")
                        break;

                    if (sym != ",")
                        throw new UnexpectedSymbol(sym, pos);

                    if (!lexer.MoveNext())
                        throw new IncompleteJSONError("Incomplete JSON data");

                    symbol = lexer.Current.Symbol;
                    pos = lexer.Current.Pos;
                    sym = symbol.ToString();
                }
            }
            
            yield return new ijsonEvent2 { Type = ijsonTokenType.EndMap, Value = null };
        }

        public IEnumerable<ijsonEvent2> BasicParse(StreamReader f, int bufsize = BUFSIZE)
        {
            using (var iter = Lexer(f, bufsize).GetEnumerator())
            {
                foreach (var value in ParseValue(iter))
                    yield return value;

                if (iter.MoveNext())
                    throw new JSONError("Additional data");
            }
        }

        public IEnumerable<ijsonEvent> Parse(StreamReader f, int bufsize = BUFSIZE)
        {
            return Common.Parse(BasicParse(f, bufsize));
        }

        public string Pretty(StreamReader f, string indent_string = "\t", string eol_string = "\r\n", int bufsize = BUFSIZE)
        {
            return Common.Pretty(BasicParse(f, bufsize), indent_string, eol_string);
        }

        public string PrettySorted(StreamReader f, string indent_string = "\t", string eol_string = "\r\n", int bufsize = BUFSIZE)
        {
            var OB = new ObjectBuilder();
            foreach (var evt in BasicParse(f, bufsize))
                OB.BuildObject(evt);

            return Common.Pretty(OB.ParseObjectSorted(OB.BuiltObject), indent_string, eol_string);
        }

        public string Minify(StreamReader f, int bufsize = BUFSIZE)
        {
            return Common.Minify(BasicParse(f, bufsize));
        }
    }
}
