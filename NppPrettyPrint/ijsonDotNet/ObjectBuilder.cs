using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ijsonDotNet
{
    class ObjectBuilder
    {
        public enum ItemType
        {
            MapItem,
            ArrayItem
        }

        public enum ValueType
        {
            MapList,
            ArrayList,
            Null,
            Boolean,
            String,
            Number
        }

        public enum ListType
        {
            Map,
            Array
        }

        public ItemList<Item> BuiltObject = null;
        private delegate void ItemSetter(ValueType type, object value);
        private Stack<ItemSetter> Container = new Stack<ItemSetter>();
        
        public interface IListType
        {
            ListType ListType { get; }
        }

        public interface IItem
        {
            ItemType ItemType { get; }
            string Key { get; set; }
            ValueType ValueType { get; set; }
            object Value { get; set; }
        }

        public abstract class ItemList<T> : List<T>, IListType where T : Item
        {
            public abstract ListType ListType { get; }
        }

        public class MapList : ItemList<Item>
        {
            public override ListType ListType { get { return ListType.Map; } }
            public string MapKey { get; set; }

            public void MapItemSetter(ValueType type, object value)
            {
                Add(new MapItem() { Key = MapKey, ValueType = type, Value = value });
            }
        }

        public class ArrayList : ItemList<Item>
        {
            public override ListType ListType { get { return ListType.Array; } }

            public void ArrayItemSetter(ValueType type, object value)
            {
                Add(new ArrayItem() { ValueType = type, Value = value });
            }
        }

        public abstract class Item : IItem, IComparable
        {
            public abstract ItemType ItemType { get; }
            public abstract string Key { get; set; }
            public abstract object Value { get; set; }
            public abstract ValueType ValueType { get; set; }
            public abstract int CompareTo(object obj);
        }

        public class MapItem : Item
        {
            public override ItemType ItemType { get { return ItemType.MapItem; } }
            public override string Key { get; set; }
            public override ValueType ValueType { get; set; }
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
            public override ValueType ValueType { get; set; }
            public override object Value { get; set; }

            public override int CompareTo(object obj)
            {
                if (obj == null) throw new ArgumentException("Object cannot be null");

                var item = obj as ArrayItem;
                if (item == null) throw new ArgumentException("Object is not an ArrayItem");

                if (item.ValueType == ValueType.MapList || item.ValueType == ValueType.ArrayList)
                {
                    return 0;
                }
                else
                {
                    if (ValueType == ValueType.MapList || ValueType == ValueType.ArrayList)
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
            if (item.ValueType == ValueType.MapList || item.ValueType == ValueType.ArrayList)
            {
                foreach (var evt in ParseList((ItemList<Item>)item.Value, sorted))
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

        public IEnumerable<ijsonEvent2> ParseList(ItemList<Item> items, bool sorted = false)
        {
            if (sorted)
                items.Sort();

            if (items.ListType == ListType.Map)
                yield return new ijsonEvent2() { Type = ijsonTokenType.StartMap, Value = null };
            else if (items.ListType == ListType.Array)
                yield return new ijsonEvent2() { Type = ijsonTokenType.StartArray, Value = null };
            else
                throw new JSONError(string.Format("Unexpected object type: {0}", items.ListType.ToString()));

            foreach (var item in items)
                foreach (var evt in ParseItem(item, sorted))
                    yield return evt;

            if (items.ListType == ListType.Map)
                yield return new ijsonEvent2() { Type = ijsonTokenType.EndMap, Value = null };
            else if (items.ListType == ListType.Array)
                yield return new ijsonEvent2() { Type = ijsonTokenType.EndArray, Value = null };
        }

        private void InitialSetter(ValueType type, object list)
        {
            BuiltObject = (ItemList<Item>)list;
        }

        public ObjectBuilder()
        {
            Container.Push(new ItemSetter(InitialSetter));
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
                Container.Peek()(ValueType.MapList, mapList);
                Container.Push(new ItemSetter(mapList.MapItemSetter));
            }
            else if (evt.Type == ijsonTokenType.StartArray)
            {
                var arrList = new ArrayList();
                Container.Peek()(ValueType.ArrayList, arrList);
                Container.Push(new ItemSetter(arrList.ArrayItemSetter));
            }
            else if (evt.Type == ijsonTokenType.EndMap || evt.Type == ijsonTokenType.EndArray)
            {
                Container.Pop();
            }
            else
            {
                Container.Peek()((ValueType)Enum.Parse(typeof(ValueType), evt.Type.ToString()), evt.Value);
            }
        }

        public IEnumerable<ijsonEvent2> ParseObject(ItemList<Item> list)
        {
            foreach (var evt in ParseList(list))
                yield return evt;
        }

        public IEnumerable<ijsonEvent2> ParseObjectSorted(ItemList<Item> list)
        {
            foreach (var evt in ParseList(list, true))
                yield return evt;
        }
    }
}
