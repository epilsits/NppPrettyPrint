using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ijsonDotNet
{
    class ObjectBuilder
    {
        public enum ItemType
        {
            Map,
            MapItem,
            Array,
            ArrayItem,
            Null,
            Boolean,
            String,
            Number
        }

        public ItemList<Item> BuiltObject = null;
        private delegate void ValueSetter(ItemType type, object value);
        private Stack<ValueSetter> Container = new Stack<ValueSetter>();
        
        public interface IItemType
        {
            ItemType ItemType { get; }
        }

        public interface IItem : IItemType
        {
            string Key { get; set; }
            ItemType ValueType { get; set; }
            object Value { get; set; }
        }

        public abstract class ItemList<T> : List<T>, IItemType where T : Item
        {
            public abstract ItemType ItemType { get; }
        }

        public class MapList : ItemList<Item>
        {
            public override ItemType ItemType { get { return ItemType.Map; } }
            public string MapKey { get; set; }

            public void MapItemSetter(ItemType type, object value)
            {
                Add(new MapItem() { Key = MapKey, ValueType = type, Value = value });
            }
        }

        public class ArrayList : ItemList<Item>
        {
            public override ItemType ItemType { get { return ItemType.Array; } }

            public void ArrayItemSetter(ItemType type, object value)
            {
                Add(new ArrayItem() { ValueType = type, Value = value });
            }
        }

        public abstract class Item : IItem, IComparable
        {
            public abstract ItemType ItemType { get; }
            public abstract string Key { get; set; }
            public abstract object Value { get; set; }
            public abstract ItemType ValueType { get; set; }
            public abstract int CompareTo(object obj);
        }

        public class MapItem : Item
        {
            public override ItemType ItemType { get { return ItemType.MapItem; } }
            public override string Key { get; set; }
            public override ItemType ValueType { get; set; }
            public override object Value { get; set; }

            public override int CompareTo(object obj)
            {
                if (obj == null) throw new ArgumentException("Object cannot be null");

                var item = obj as MapItem;
                if (item == null) throw new ArgumentException("Object is not a MapItem");

                return Key.CompareTo(item.Key);
            }
        }

        public class ArrayItem : Item
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int StrCmpLogicalW(string x, string y);

            public override ItemType ItemType { get { return ItemType.ArrayItem; } }
            public override string Key { get; set; }
            public override ItemType ValueType { get; set; }
            public override object Value { get; set; }

            public override int CompareTo(object obj)
            {
                if (obj == null) throw new ArgumentException("Object cannot be null");

                var item = obj as ArrayItem;
                if (item == null) throw new ArgumentException("Object is not an ArrayItem");

                if (item.ValueType == ItemType.Map || item.ValueType == ItemType.Array)
                {
                    return 0;
                }
                else
                {
                    if (ValueType == ItemType.Map || ValueType == ItemType.Array)
                    {
                        return 1;
                    }
                    else
                    {
                        var v1 = (Value == null) ? "" : Value.ToString();
                        var v2 = (item.Value == null) ? "" : item.Value.ToString();
                        return StrCmpLogicalW(v1, v2);
                    }
                }
            }
        }

        public IEnumerable<ijsonEvent2> ParseValue(Item item, bool sorted = false)
        {
            if (item.ValueType == ItemType.Map || item.ValueType == ItemType.Array)
            {
                foreach (var evt in ParseObject((ItemList<Item>)item.Value, sorted))
                    yield return evt;
            }
            else // value
            {
                yield return new ijsonEvent2() { Type = (ijsonTokenType)Enum.Parse(typeof(ijsonTokenType), item.ValueType.ToString()), Value = item.Value };
            }
        }

        public IEnumerable<ijsonEvent2> ParseItem(Item item, bool sorted = false)
        {
            if (item.ItemType == ItemType.MapItem)
            {
                yield return new ijsonEvent2() { Type = ijsonTokenType.MapKey, Value = item.Key };
                foreach (var evt in ParseValue(item, sorted))
                    yield return evt;
            }
            else // ArrayItem, base types
            {
                foreach (var evt in ParseValue(item, sorted))
                    yield return evt;
            }
        }

        public IEnumerable<ijsonEvent2> ParseObject(ItemList<Item> items, bool sorted = false)
        {
            if (sorted)
                items.Sort();

            if (items.ItemType == ItemType.Map)
                yield return new ijsonEvent2() { Type = ijsonTokenType.StartMap, Value = null };
            else if (items.ItemType == ItemType.Array)
                yield return new ijsonEvent2() { Type = ijsonTokenType.StartArray, Value = null };
            else
                throw new JSONError(string.Format("Unexpected object type: {0}", items.ItemType.ToString()));

            foreach (var item in items)
                foreach (var evt in ParseItem(item, sorted))
                    yield return evt;

            if (items.ItemType == ItemType.Map)
                yield return new ijsonEvent2() { Type = ijsonTokenType.EndMap, Value = null };
            else if (items.ItemType == ItemType.Array)
                yield return new ijsonEvent2() { Type = ijsonTokenType.EndArray, Value = null };
        }

        private void InitialSetter(ItemType type, object list)
        {
            BuiltObject = (ItemList<Item>)list;
        }

        public ObjectBuilder()
        {
            Container.Push(new ValueSetter(InitialSetter));
        }

        public void BuildObject(ijsonEvent2 evt)
        {
            if (evt.Type == ijsonTokenType.MapKey)
            {
                ((MapList)Container.Peek().Target).MapKey = evt.Value.ToString();
            }
            else if (evt.Type == ijsonTokenType.StartMap)
            {
                var mapList = new MapList();
                Container.Peek()(ItemType.Map, mapList);
                Container.Push(new ValueSetter(mapList.MapItemSetter));
            }
            else if (evt.Type == ijsonTokenType.StartArray)
            {
                var arrList = new ArrayList();
                Container.Peek()(ItemType.Array, arrList);
                Container.Push(new ValueSetter(arrList.ArrayItemSetter));
            }
            else if (evt.Type == ijsonTokenType.EndMap || evt.Type == ijsonTokenType.EndArray)
            {
                Container.Pop();
            }
            else
            {
                Container.Peek()((ItemType)Enum.Parse(typeof(ItemType), evt.Type.ToString()), evt.Value);
            }
        }

        public IEnumerable<ijsonEvent2> SortedObject(IEnumerable<ijsonEvent2> events)
        {
            foreach (var evt in events)
                BuildObject(evt);

            foreach (var evt in ParseObject(BuiltObject, true))
                yield return evt;
        }
    }
}
