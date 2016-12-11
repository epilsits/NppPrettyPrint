using System;

namespace ijsonDotNet
{
    public class ijsonEvent
    {
        public string Prefix { get; set; }
        public ijsonTokenType Type { get; set; }
        public object Value { get; set; }
    }

    public class ijsonEvent2
    {
        public ijsonTokenType Type { get; set; }
        public object Value { get; set; }
    }

    public class ijsonLexerEvent
    {
        public int Pos { get; set; }
        public string Symbol { get; set; }
    }

    public class JSONError : Exception
    {
        public JSONError() { }
        public JSONError(string msg) : base(msg) { }
        public JSONError(string msg, Exception inner) : base(msg, inner) { }
    }

    public class IncompleteJSONError : JSONError
    {
        public IncompleteJSONError() { }
        public IncompleteJSONError(string msg) : base(msg) { }
        public IncompleteJSONError(string msg, Exception inner) : base(msg, inner) { }
    }

    public class UnexpectedSymbol : JSONError
    {
        public UnexpectedSymbol() { }
        public UnexpectedSymbol(string symbol) : base(string.Format("Unexpected symbol {0}", symbol)) { }
        public UnexpectedSymbol(string symbol, int pos) : base(string.Format("Unexpected symbol {0} at {1}", symbol, pos)) { }
        public UnexpectedSymbol(string symbol, Exception inner) : base(string.Format("Unexpected symbol {0}", symbol), inner) { }
    }

    public enum ijsonTokenType
    {
        None,
        StartMap,
        EndMap,
        MapKey,
        StartArray,
        EndArray,
        Null,
        Boolean,
        String,
        Number
    }
}