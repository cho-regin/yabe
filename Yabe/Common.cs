using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.BACnet;
using System.Linq;

namespace Yabe
{
    /// <summary>
    /// Common used stuff.
    /// </summary>
    internal static class Common
    {
        static Common()
        {
            var unitLoader = new CsvLoader<BacnetUnitsId>(Properties.Resources.Units, 0);
            Common.Unit_Shortcuts = unitLoader.CreateDictionary(1);
            Common.Unit_EdeTexts = unitLoader.CreateDictionary(5);

            var objTypeLoader = new CsvLoader<BacnetObjectTypes>(Properties.Resources.ObjectTypes, 0);
            Common.ObjectType_EdeTexts = objTypeLoader.CreateDictionary(3);
        }


        public static IReadOnlyDictionary<BacnetUnitsId, string> Unit_Shortcuts { get; }
        public static IReadOnlyDictionary<BacnetUnitsId, string> Unit_EdeTexts { get; }
        public static IReadOnlyDictionary<BacnetObjectTypes, string> ObjectType_EdeTexts { get; }
    }


    /// <summary>
    /// Loader for reading text from *.csv files.
    /// Containing data can be obtained per row. Each cell will be associated with an enum value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CsvLoader<T>
        where T : struct, Enum
    {
        /// <summary>
        /// Creates a new loader instance.
        /// </summary>
        /// <param name="content">Content to load.</param>
        /// <param name="primaryKeyColumn">Index of column that provides primary keys to associate with the enum type <typeparamref name="T"/>.</param>
        /// <param name="firstRow">Index of the first data row.</param>
        public CsvLoader(string content, int primaryKeyColumn, int firstRow = 1)
        {
            this.Lines = content.Split('\n');
            this.PrimaryKeyColumn = primaryKeyColumn;
            this.FirstRow = firstRow;
        }


        private string[] Lines { get; }
        private int PrimaryKeyColumn { get; }
        private int FirstRow { get; }


        /// <summary>
        /// Create dictionary from loaded data.
        /// </summary>
        public Dictionary<T, string> CreateDictionary(int column, bool skipEmptyCells = true)
        {
            var result = new Dictionary<T, string>();
            int i = 0;
            foreach (var line in Lines)
            {
                if (i++ < FirstRow)
                    continue;
                if (line.Length == 0)
                    continue;
                var cells = line.Split(';');
                if (PrimaryKeyColumn >= cells.Length)
                    throw new IndexOutOfRangeException($"Creating dictionary failed at reading primary key at line {i}!");
                if (!int.TryParse(cells[PrimaryKeyColumn], out var key))
                    throw new InvalidCastException($"Creating dictionary failed at parsing primary key at line {i}!");
                string val = "";
                if (column < cells.Length)
                    val = cells[column].Replace("\r", "");
                if ((val.Length > 0) || (!skipEmptyCells))
                    result.Add((T)Enum.ToObject(typeof(T), key), val);
            }
            return (result);
        }
    }
}

namespace Utilities
{
    internal static class Extensions
    {
        #region Object
        public static T ChangeType<T>(this object source)
        {
            if (source is T _var)
                return (_var);
            else
            {
                var _type = typeof(T);
                try
                {
                    // Handle nullable types (int?, double?, ...)
                    if (Nullable.GetUnderlyingType(_type) != null)
                        return ((T)TypeDescriptor.GetConverter(_type).ConvertFrom(source));
                    else
                        return ((T)Convert.ChangeType(source, _type));
                }
                catch (Exception)
                {
                    return (default(T));
                }
            }
        }
        #endregion
        #region Enumerable
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] items)
        {
            foreach (var item in source)
                yield return (item);
            foreach (var item in items)
                yield return (item);
            yield break;
        }
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return ((source == null) || (!source.Any()));
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
                action(item);
        }
        public static IEnumerable<TNew> ChangeType<T, TNew>(this IEnumerable<T> source)
        {
            var type = typeof(TNew);
            if (type.IsEnum)
                return (source.Select(item => (TNew)Enum.ToObject(type, item)));
            else
                return (source.Select(item => item.ChangeType<TNew>()));
        }
        public static T[] ToArray<T>(this IEnumerable<object> source) => (T[])ToArray(source, typeof(T));
        public static Array ToArray(this IEnumerable<object> source, Type type)
        {
            if (source.GetType() == type)
                return ((Array)source);
            else
            {
                var objArray = source.ToArray();
                var res = Array.CreateInstance(type, objArray.Count());
                var i = 0;
                objArray.ForEach(val => res.SetValue(val, i++));
                return (res);
            }
        }
        public static IList ToList(this IEnumerable<object> source, Type type)
        {
            if (source.GetType() == type)
                return ((IList)source);
            else
            {
                var listType = typeof(List<>).MakeGenericType(type);
                var list = (IList)Activator.CreateInstance(listType);
                {
                    list.AddRange(source);
                }
                return (list);
            }
        }
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                if (item != null)
                    yield return (item);
            }
            yield break;
        }
        #endregion
        #region Collection
        public static ICollection<T> AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            foreach (var item in items)
                source.Add(item);
            return (source);
        }
        #endregion
        #region List
        public static IList AddRange(this IList source, IEnumerable items)
        {
            foreach (var item in items)
                source.Add(item);
            return (source);
        }
        public static bool AddIfSucceeded<T>(this IList<T> source, (bool, T) value)
        {
            if (value.Item1)
                source.Add(value.Item2);
            return (value.Item1);
        }
        #endregion
        #region Dictionary
        /// <summary>
        /// Updates the actual value of the specified <paramref name="key"/> with a new <paramref name="value"/> or (if the key does not exist) adds the new <paramref name="value"/>.
        /// </summary>
        public static IDictionary<TKey, TVal> AddOrUpdate<TKey, TVal>(this IDictionary<TKey, TVal> source, TKey key, TVal value)
        {
            if (source.ContainsKey(key))
                source[key] = value;
            else
                source.Add(key, value);

            return (source);
        }
        /// <summary>
        /// Gets the value of the specified key or creates (and add to the dictionary) a new value instance.
        /// </summary>
        public static TVal GetOrCreateValue<TKey, TVal>(this IDictionary<TKey, TVal> source, TKey key)
        {
            TVal res;
            if (!source.TryGetValue(key, out res))
            {
                res = (TVal)Activator.CreateInstance(typeof(TVal));
                source.Add(key, res);
            }
            return (res);
        }
        /// <inheritdoc cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
        /// <typeparam name="TRes">Expected result type.</typeparam>
        public static bool TryGetValue<TKey, TVal, TRes>(this IDictionary<TKey, TVal> source, TKey key, out TRes value)
        {
            if (source.TryGetValue(key, out var val))
            {
                value = val.ChangeType<TRes>();
                return (true);
            }
            else
            {
                value = default;
                return (false);
            }
        }
        #endregion
    }
}
