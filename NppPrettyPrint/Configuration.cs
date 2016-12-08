using System;

namespace Configuration
{
    public interface ISetting
    {
        string name { get; set; }
    }

    public interface IValueSetting<T> : ISetting
    {
        T value { get; set; }
        string ValToString();
        bool ValToBool();
        int ValToInt();
        T ValNot();
        void SetValue(int i);
        void SetValue(bool i);
    }

    public class IntSetting : IValueSetting<int>
    {
        public string name { get; set; }
        public int value { get; set; }

        public IntSetting() { }

        public IntSetting(string _name)
        {
            name = _name;
        }

        public IntSetting(string _name, int _value)
        {
            name = _name;
            value = _value;
        }

        public override string ToString()
        {
            return name;
        }

        public string ValToString()
        {
            return value.ToString();
        }

        public bool ValToBool()
        {
            return ((value == 0) ? false : true);
        }

        public int ValToInt()
        {
            return value;
        }

        public int ValNot()
        {
            return ((value == 0) ? 1 : 0);
        }

        public void SetValue(int i)
        {
            value = i;
        }

        public void SetValue(bool b)
        {
            value = (b ? 1 : 0);
        }
    }

    public class BoolSetting : IValueSetting<bool>
    {
        public string name { get; set; }
        public bool value { get; set; }

        public BoolSetting() { }

        public BoolSetting(string _name)
        {
            name = _name;
        }

        public BoolSetting(string _name, bool _value)
        {
            name = _name;
            value = _value;
        }

        public override string ToString()
        {
            return name;
        }

        public string ValToString()
        {
            return (value ? "1" : "0");
        }

        public bool ValToBool()
        {
            return value;
        }

        public int ValToInt()
        {
            return (value ? 1 : 0);
        }

        public bool ValNot()
        {
            return !value;
        }

        public void SetValue(int i)
        {
            value = ((i == 0) ? false : true);
        }

        public void SetValue(bool b)
        {
            value = b;
        }
    }

    public class AutoSetting<T, U> where T : IValueSetting<U>, new()
    {
        public T setting { get; set; }
        public dynamic value { get { return setting.value; } set { setting.SetValue(value); } }

        public AutoSetting(T s)
        {
            setting = s;
        }

        public override string ToString()
        {
            return setting.name;
        }

        public string ValToString()
        {
            return setting.ValToString();
        }

        public static implicit operator string(AutoSetting<T, U> s)
        {
            return s.setting.name;
        }

        public static implicit operator bool(AutoSetting<T, U> s)
        {
            return s.setting.ValToBool();
        }

        public static implicit operator int(AutoSetting<T, U> s)
        {
            return s.setting.ValToInt();
        }

        public static AutoSetting<T, U> operator !(AutoSetting<T, U> s)
        {
            return new AutoSetting<T, U>(new T() { name = s.setting.name, value = s.setting.ValNot() });
        }
    }
}