﻿using dexih.functions.Query;
using Dexih.Utils.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using static Dexih.Utils.DataType.DataType;

namespace dexih.functions
{
    
    [Serializable]
    public class Table
    {

        #region Initializers
        public Table()
        {
            Data = new TableCache(0);
            Columns = new TableColumns();
        }

        public Table(string tableName, TableColumns columns, TableCache data) 
        {
            Name = tableName;
            LogicalName = DefaultLogicalName();
            BaseTableName = CleanString(tableName);
            Columns = columns??new TableColumns();
            Data = data;
        }

        public Table(string tableName, int maxRows, params TableColumn[] columns) 
        {
            Name = tableName;
            LogicalName = DefaultLogicalName();
            BaseTableName = CleanString(tableName);
            Columns = new TableColumns();
            foreach (var column in columns)
                Columns.Add(column);

            Data = new TableCache(maxRows);
        }

        public Table(string tableName, int maxRows, TableColumns columns)
        {
            Name = tableName;
            LogicalName = DefaultLogicalName();
            BaseTableName = CleanString(tableName);
            Columns = columns;
            Data = new TableCache(maxRows);
        }

		public Table(string tableName, int maxRows = 0)
		{
		    Name = tableName;
            LogicalName = DefaultLogicalName();
            BaseTableName = CleanString(tableName);
			Columns = new TableColumns();
			Data = new TableCache(maxRows);
		}

        public Table(string tableName, string schema, int maxRows = 0) 
        {
            Name = tableName;
			Schema = schema;
            LogicalName = DefaultLogicalName();
            BaseTableName = CleanString(tableName);
            Columns = new TableColumns();
            Data = new TableCache(maxRows);
        }

        protected string DefaultLogicalName()
        {
            var name = Name.Replace("\"", "");

            if (string.IsNullOrEmpty(Schema))
            {
                return name;
            }
            else
            {
                return Schema + "." + name;
            }
        }

        /// <summary>
        /// Removes all non alphanumeric characters from the string
        /// </summary>
        /// <returns></returns>
        private string CleanString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var arr = value.Where(c => (char.IsLetterOrDigit(c))).ToArray();
            var newValue = new string(arr);
            return newValue;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Reference to the phsycal table name.
        /// </summary>
        public string Name { get; set; }

		/// <summary>
		/// The table schema or owner.
		/// </summary>
		/// <value>The table schema.</value>
		public string Schema { get; set; }

        /// <summary>
        /// The name of the source connection when pointing to an another hub
        /// </summary>
        /// <value>The table connection.</value>
        public string SourceConnectionName { get; set; }

        /// <summary>
        /// A logical name for the table.
        /// </summary>
        public string LogicalName { get; set; }

        /// <summary>
        /// Table description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Is the original base table name.
        /// </summary>
        public string BaseTableName { get; set; }
        
        /// <summary>
        /// Indicates if the table contains versions (history) of data change, such as sql temporal tables.
        /// </summary>
        public bool IsVersioned { get; set; }

        /// <summary>
        /// Indicates if this is a sql (or other) type query.
        /// </summary>
        public bool UseQuery { get; set; }

        /// <summary>
        /// Sql Query string (or query string for other db types)
        /// </summary>
        public string QueryString { get; set; }
     
        /// <summary>
        /// Indicates the output sort fields for the table.
        /// </summary>
        /// <returns></returns>
        public virtual List<Sort> OutputSortFields { get; set; }

        /// <summary>
        /// Indicates the key that should be used when running update/delete operations against the target.
        /// </summary>
        public List<string> KeyFields { get; set; }

        public TableCache Data { get; set; }

        public TableColumns Columns { get; set; }

		// public Dictionary<string, string> ExtendedProperties { get; set; }

        public TableColumn this[string columnName] => Columns[columnName];

        public TableColumn this[string columnName, string columnGroup] => Columns[columnName, columnGroup];

        /// <summary>
        /// Maximum levels to recurse through structured data when importing columns.
        /// </summary>
        public int MaxImportLevels { get; set; } = 1;


        #endregion

        #region Lookup

        /// <summary>
        /// Performs a row scan lookup on the data contained in the table.
        /// </summary>
        /// <param name="filters">Filter for the lookup.  For an index to be used, the filters must be in the same column order as the index.</param>
        /// <param name="startRow"></param>
        /// <returns></returns>
        public object[] LookupSingleRow(List<Filter> filters, int startRow = 0)
        {
            try
            {
                //scan the data for a matching row.  
                //TODO add indexing to lookup process.
                for (var i = startRow; i < Data.Count(); i++)
                {
                    if (RowMatch(filters, Data[i]))
                        return Data[i];
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new TableException("The lookup row failed.  " + ex.Message, ex);
            }
        }

        public List<object[]> LookupMultipleRows(List<Filter> filters, int startRow = 0)
        {
            try
            {
                List<object[]> rows = null;

                //scan the data for a matching row.  
                //TODO add indexing to lookup process.
                for (var i = startRow; i < Data.Count(); i++)
                {
                    if (RowMatch(filters, Data[i]))
                    {
                        if (rows == null)
                            rows = new List<object[]>();
                        rows.Add(Data[i]);
                    }
                }

                return rows;
            }
            catch (Exception ex)
            {
                throw new TableException("The lookup multiple rows failed.  " + ex.Message, ex);
            }
        }



        public bool RowMatch(IEnumerable<Filter> filters, object[] row)
        {
            var isMatch = true;

            foreach (var filter in filters)
            {
                object value1;
                object value2;

                if (filter.Column1 == null)
                {
                    value1 = filter.Value1;
                }
                else
                {
                    value1 = row[GetOrdinal(filter.Column1.Name)];
                }

                if (filter.Column2 == null)
                    value2 = filter.Value2;
                else
                {
                    value2 = row[GetOrdinal(filter.Column2.Name)];
                }

                switch (filter.Operator)
                {
                    case Filter.ECompare.IsEqual:
                        isMatch = Operations.Equal(filter.CompareDataType, value1, value2);
                        break;
                    case Filter.ECompare.NotEqual:
                        isMatch = !Operations.Equal(filter.CompareDataType, value1, value2);
                        break;
                    case Filter.ECompare.LessThan:
                        isMatch = Operations.LessThan(filter.CompareDataType, value1, value2);
                        break;
                    case Filter.ECompare.LessThanEqual:
                        isMatch = Operations.LessThanOrEqual(filter.CompareDataType, value1, value2);
                        break;
                    case Filter.ECompare.GreaterThan:
                        isMatch = Operations.GreaterThan(filter.CompareDataType, value1, value2);
                        break;
                    case Filter.ECompare.GreaterThanEqual:
                        isMatch = Operations.GreaterThanOrEqual(filter.CompareDataType, value1, value2);
                        break;
                }

                if (!isMatch)
                    break;
            }

            return isMatch;
        }
        #endregion


        /// <summary>
        /// Adds the standard set of audit columns to the table.  
        /// </summary>
        public void AddAuditColumns()
        {

            //add the audit columns if they don't exist
            //get some of the key fields to save looking up for each row.
            var colValidFrom = GetDeltaColumn(TableColumn.EDeltaType.ValidFromDate);
            var colValidTo = GetDeltaColumn(TableColumn.EDeltaType.ValidToDate);
            var colCreateDate = GetDeltaColumn(TableColumn.EDeltaType.CreateDate);
            var colUpdateDate = GetDeltaColumn(TableColumn.EDeltaType.UpdateDate);
            var colSurrogateKey = GetDeltaColumn(TableColumn.EDeltaType.SurrogateKey);
            var colIsCurrentField = GetDeltaColumn(TableColumn.EDeltaType.IsCurrentField);
            var colVersionField = GetDeltaColumn(TableColumn.EDeltaType.Version);
            var colCreateAuditKey = GetDeltaColumn(TableColumn.EDeltaType.CreateAuditKey);
            var colUpdateAuditKey = GetDeltaColumn(TableColumn.EDeltaType.UpdateAuditKey);
//            var colSourceSurrogateKey = GetDeltaColumn(TableColumn.EDeltaType.SourceSurrogateKey);
//            var colRejectedReason = GetDeltaColumn(TableColumn.EDeltaType.RejectedReason);

            if (colValidFrom == null)
            {
                colValidFrom = new TableColumn("ValidFromDate", ETypeCode.DateTime) { DeltaType = TableColumn.EDeltaType.ValidFromDate };
                Columns.Add(colValidFrom);
            }
            
            if (colValidTo == null)
            {
                colValidTo = new TableColumn("ValidToDate", ETypeCode.DateTime) { DeltaType = TableColumn.EDeltaType.ValidToDate };
                Columns.Add(colValidTo);
            }
            
            if (colCreateDate == null)
            {
                colCreateDate = new TableColumn("CreateDate", ETypeCode.DateTime) { DeltaType = TableColumn.EDeltaType.CreateDate };
                Columns.Add(colCreateDate);
            }
            
            if (colUpdateDate == null)
            {
                colUpdateDate = new TableColumn("UpdateDate", ETypeCode.DateTime) { DeltaType = TableColumn.EDeltaType.UpdateDate };
                Columns.Add(colUpdateDate);
            }
            
            if (colSurrogateKey == null)
            {
                colSurrogateKey = new TableColumn("SurrogateKey", ETypeCode.Int64) { DeltaType = TableColumn.EDeltaType.SurrogateKey };
                Columns.Add(colSurrogateKey);
            }

            if (colIsCurrentField == null)
            {
                colIsCurrentField = new TableColumn("IsCurrent", ETypeCode.Boolean) { DeltaType = TableColumn.EDeltaType.IsCurrentField };
                Columns.Add(colIsCurrentField);
            }

            if (colVersionField == null)
            {
                colVersionField = new TableColumn("Version", ETypeCode.Int32) { DeltaType = TableColumn.EDeltaType.Version };
                Columns.Add(colVersionField);
            }
            
            if (colCreateAuditKey == null)
            {
                colCreateAuditKey = new TableColumn("CreateAuditKey", ETypeCode.Int64) { DeltaType = TableColumn.EDeltaType.CreateAuditKey };
                Columns.Add(colCreateAuditKey);
            }

            if (colUpdateAuditKey == null)
            {
                colUpdateAuditKey = new TableColumn("UpdateAuditKey", ETypeCode.Int64) { DeltaType = TableColumn.EDeltaType.UpdateAuditKey };
                Columns.Add(colUpdateAuditKey);
            }
        }

        /// <summary>
        /// Create as a rejected table based on the current table.
        /// </summary>
        /// <returns></returns>
        public Table GetRejectedTable(string rejectedTableName)
        {
            if (string.IsNullOrEmpty(rejectedTableName)) return null;

            var table = Copy();

            // reset delta types on reject table to ensure the reject table contains no keys.
            foreach(var column in table.Columns)
            {
                column.DeltaType = TableColumn.EDeltaType.TrackingField;
            }

            table.Name = rejectedTableName;
            table.Description = "Rejected table for: " + Description;

            if(GetDeltaColumn(TableColumn.EDeltaType.RejectedReason) == null)
                table.Columns.Add(new TableColumn("RejectedReason", ETypeCode.String, TableColumn.EDeltaType.RejectedReason));

            return table;
        }

        /// <summary>
        /// Gets the secured version of the table, with columns tagged as secured set to appropriate string type
        /// </summary>
        /// <returns></returns>
        public Table GetSecureTable()
        {
            var table = Copy();

            foreach(var column in table.Columns)
            {
                column.SecurityFlag = Columns[column.Name].SecurityFlag;
                if(column.SecurityFlag != TableColumn.ESecurityFlag.None)
                {
                    column.DataType = ETypeCode.String;
                    column.MaxLength = 250;
                }
            }

            return table;
        }

        /// <summary>
        /// Creates a copy of the table, excluding cached data, and sort columns
        /// </summary>
        /// <returns></returns>
        public Table Copy(bool removeSchema = false, bool removeIgnoreColumns = false)
        {
            var table = new Table(Name, Schema)
            {
                Description = Description,
                LogicalName = LogicalName
            };


            foreach (var column in Columns)
            {
                if (!removeIgnoreColumns || column.DeltaType != TableColumn.EDeltaType.IgnoreField)
                {
                    var newCol = column.Copy();
                    if (removeSchema) newCol.ReferenceTable = null;

                    table.Columns.Add(newCol);
                }
            }

            return table;
        }

        public void AddColumn(string columnName, ETypeCode dataType = ETypeCode.String, TableColumn.EDeltaType deltaType = TableColumn.EDeltaType.TrackingField, byte arrayDimensions = 0)
        {
            if (Columns == null)
                Columns = new TableColumns();

            Columns.Add(new TableColumn(columnName, dataType, deltaType, arrayDimensions, Name));
        }

        public void AddRow(params object[] values)
        {
            if (values.Length != Columns.Count())
                throw new Exception("The number of parameters must match the number of columns (" + Columns.Count + ") precisely.");

            var row = new object[Columns.Count];
            values.CopyTo(row, 0);

            Data.Add(values);
        }

        public int GetOrdinal(string schemaColumnName)
        {
            return Columns.GetOrdinal(schemaColumnName);
        }

		public int GetOrdinal(TableColumn column)
		{
			var ordinal = GetOrdinal(column.TableColumnName());
			if(ordinal < 0) 
			{
				ordinal = GetOrdinal(column.Name);
			}

			return ordinal;
		}

        public int[] GetOrdinalPath(string schemaColumnName, TableColumns columns = null)
        {
            if (columns == null)
            {
                columns = Columns;
            }

            var ordinal = columns.GetOrdinal(schemaColumnName);
            if (ordinal >= 0)
            {
                return new[] {ordinal};
            }

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var ordinals = GetOrdinalPath(schemaColumnName, column.ChildColumns);

                if (ordinals.Length > 0)
                {
                    return ordinals.Prepend(i).ToArray();
                }
            }

            return new int[0];
        }

        public TableColumn GetDeltaColumn(TableColumn.EDeltaType deltaType)
        {
            try
            {
                return Columns.SingleOrDefault(c => c.DeltaType == deltaType);
            }
            catch(Exception ex)
            {
                throw new TableException($"The column with the deltaType {deltaType} could not be determined.  {ex.Message}", ex);
            }
        }

        public int GetDeltaColumnOrdinal(TableColumn.EDeltaType deltaType)
        {
            for (var i = 0; i < Columns.Count; i++)
                if (Columns[i].DeltaType == deltaType)
                    return i;

            return -1;
        }

        public TableColumn[] GetColumnsByDeltaType(TableColumn.EDeltaType deltaType)
        {
            var columns = (from s in Columns where s.DeltaType == deltaType select s).ToArray();
            return columns;
        }
        
        public TableColumn GetIncrementalUpdateColumn()
        {
            return Columns.SingleOrDefault(c => c.IsIncrementalUpdate);
        }

        //creates a simple select query with all fields and no sorts, filters
        public SelectQuery DefaultSelectQuery(int rows = -1)
        {
            return new SelectQuery
            {
                Columns = Columns.Where(c=>c.DeltaType != TableColumn.EDeltaType.IgnoreField && c.DataType != ETypeCode.Unknown).Select(c => new SelectColumn(c)).ToList(),
                Table = Name,
                Rows = rows
            };
        }

        public string GetCsv()
        {
            var csvData = new StringBuilder();

            var columns = Columns.Select(c => c.Name).ToArray();
            //add column headers
            var columnCount = Columns.Count;
            var s = new string[columnCount];
            for (var j = 0; j < columnCount; j++)
            {
                s[j] = columns[j];
                if (s[j].Contains("\"")) //replace " with ""
                    s[j].Replace("\"", "\"\"");
                if (s[j].Contains("\"") || s[j].Contains(" ")) //add "'s around any string with space or "
                    s[j] = "\"" + s[j] + "\"";
            }
            csvData.AppendLine(string.Join(",", s));

            //add rows
            foreach (var row in Data)
            {
                for (var j = 0; j < columnCount; j++)
                {
                    s[j] = row[j] == null ? "" : row[j].ToString();
                    if (s[j].Contains("\"")) //replace " with ""
                        s[j].Replace("\"", "\"\"");
                    if (s[j].Contains("\"") || s[j].Contains(" ")) //add "'s around any string with space or "
                        s[j] = "\"" + s[j] + "\"";
                }
                csvData.AppendLine(string.Join(",", s));
            }

            return csvData.ToString();
        }

    }
}
