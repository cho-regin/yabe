using System;
using System.Collections.Generic;
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
