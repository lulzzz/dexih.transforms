﻿using System;
using dexih.functions.Query;
using Dexih.Utils.DataType;

namespace dexih.functions.Mappings
{
    public class MapAggregate: Mapping
    {
        public MapAggregate(TableColumn inputColumn, TableColumn outputColumn, SelectColumn.EAggregate aggregate = SelectColumn.EAggregate.Sum)
        {
            InputColumn = inputColumn;
            OutputColumn = outputColumn;
            Aggregate = aggregate;
        }

        public TableColumn InputColumn;
        public TableColumn OutputColumn;
        public SelectColumn.EAggregate Aggregate { get; set; }

        private int _inputOrdinal;
        private int _outputOrdinal;

        public long Count { get; set; }
        public object Value { get; set; }

        public override void InitializeInputOrdinals(Table table, Table joinTable = null)
        {
            _inputOrdinal = table.GetOrdinal(InputColumn);
        }
        
        public override void AddOutputColumns(Table table)
        {
            table.Columns.Add(OutputColumn);
            _outputOrdinal = table.Columns.Count - 1;
        }

        public override bool ProcessInputRow(object[] rowData, object[] joinRow = null)
        {
            Count++;
            var value = _inputOrdinal == -1 ? InputColumn.DefaultValue : rowData[_inputOrdinal];
            
            if(Value == null && value != null)
            {
                Value = value;
            }
            else
            {
                switch (Aggregate)
                {
                    case SelectColumn.EAggregate.Sum:
                    case SelectColumn.EAggregate.Average:
                        Value = DataType.Add(InputColumn.DataType, Value ?? 0, value);
                        break;
                    case SelectColumn.EAggregate.Min:
                        var compare = DataType.Compare(InputColumn.DataType, value, Value);
                        if (compare == DataType.ECompareResult.Less)
                        {
                            Value = value;
                        }
                        break;
                    case SelectColumn.EAggregate.Max:
                        var compare1 = DataType.Compare(InputColumn.DataType, value, Value);
                        if (compare1 == DataType.ECompareResult.Greater)
                        {
                            Value = value;
                        }
                        break;
                }
            }
            
            return true;
        }

        public override void ProcessOutputRow(object[] row)
        {
            return;
        }

        public override void ProcessResultRow(int index, object[] row)
        {
            object value = null;
            switch (Aggregate)
            {
                case SelectColumn.EAggregate.Count:
                    value = Count;
                    break;
                case SelectColumn.EAggregate.Max:
                case SelectColumn.EAggregate.Min:
                case SelectColumn.EAggregate.Sum:
                    value = Value;
                    break;
                case SelectColumn.EAggregate.Average:
                    value = Count == 0 ? 0 : DataType.Divide(OutputColumn.DataType, Value, Count);
                    break;
            }
            row[_outputOrdinal] = value;        
        }


        public override object GetInputValue(object[] row = null)
        {
            throw new NotSupportedException();
        }

        public override void Reset()
        {
            Value = null;
            Count = 0;
        }
    }
}