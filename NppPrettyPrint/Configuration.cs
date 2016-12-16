using System;

namespace Configuration
{
    public interface ISetting
    {
        string Name { get; set; }
    }

    public interface IValueSetting<T> : ISetting
    {
        T Value { get; set; }
        string ValToString();
        bool ValToBool();
        int ValToInt();
        T ValNot();
        void SetValue(int i);
        void SetValue(bool i);
    }

    public class IntSetting : IValueSetting<int>
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public IntSetting() { }

        public IntSetting(string _name)
        {
            Name = _name;
        }

        public IntSetting(string _name, int _value)
        {
            Name = _name;
            Value = _value;
        }

        public override string ToString()
        {
            return Name;
        }

        public string ValToString()
        {
            return Value.ToString();
        }

        public bool ValToBool()
        {
            return ((Value == 0) ? false : true);
        }

        public int ValToInt()
        {
            return Value;
        }

        public int ValNot()
        {
            return ((Value == 0) ? 1 : 0);
        }

        public void SetValue(int i)
        {
            Value = i;
        }

        public void SetValue(bool b)
        {
            Value = (b ? 1 : 0);
        }
    }

    public class BoolSetting : IValueSetting<bool>
    {
        public string Name { get; set; }
        public bool Value { get; set; }

        public BoolSetting() { }

        public BoolSetting(string _name)
        {
            Name = _name;
        }

        public BoolSetting(string _name, bool _value)
        {
            Name = _name;
            Value = _value;
        }

        public override string ToString()
        {
            return Name;
        }

        public string ValToString()
        {
            return (Value ? "1" : "0");
        }

        public bool ValToBool()
        {
            return Value;
        }

        public int ValToInt()
        {
            return (Value ? 1 : 0);
        }

        public bool ValNot()
        {
            return !Value;
        }

        public void SetValue(int i)
        {
            Value = ((i == 0) ? false : true);
        }

        public void SetValue(bool b)
        {
            Value = b;
        }
    }

    public class StringSetting : IValueSetting<string>
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public StringSetting() { }

        public StringSetting(string _name)
        {
            Name = _name;
        }

        public StringSetting(string _name, string _value)
        {
            Name = _name;
            Value = _value;
        }

        public void SetValue(bool i)
        {
            Value = i ? "true" : "false";
        }

        public void SetValue(int i)
        {
            Value = i.ToString();
        }

        public void SetValue(string s)
        {
            Value = s;
        }

        public string ValNot()
        {
            throw new NotImplementedException();
        }

        public bool ValToBool()
        {
            return (Value == "") ? false : true;
        }

        public int ValToInt()
        {
            int i;
            if (int.TryParse(Value, out i))
                return i;
            else
                throw new ArgumentException("String in not an integer");
        }

        public string ValToString()
        {
            return Value;
        }
    }

    public class AutoSetting<T, U> where T : IValueSetting<U>, new()
    {
        public T Setting { get; set; }
        public dynamic Value { get { return Setting.Value; } set { Setting.SetValue(value); } }

        public AutoSetting(T s)
        {
            Setting = s;
        }

        public override string ToString()
        {
            return Setting.Name;
        }

        public string ValToString()
        {
            return Setting.ValToString();
        }

        public static implicit operator string(AutoSetting<T, U> s)
        {
            return s.Setting.Name;
        }

        public static implicit operator bool(AutoSetting<T, U> s)
        {
            return s.Setting.ValToBool();
        }

        public static implicit operator int(AutoSetting<T, U> s)
        {
            return s.Setting.ValToInt();
        }

        public static AutoSetting<T, U> operator !(AutoSetting<T, U> s)
        {
            return new AutoSetting<T, U>(new T() { Name = s.Setting.Name, Value = s.Setting.ValNot() });
        }
    }
}