﻿using dexih.functions;

namespace dexih.functions.Mappings
{
    public abstract class Mapping
    {
        /// <summary>
        /// Aligns the input value with the table ordinals.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="secondTable"></param>
        public abstract void InitializeInputOrdinals(Table table, Table joinTable = null);
        
        /// <summary>
        /// Add the mapping columns to the table.
        /// </summary>
        /// <param name="table"></param>
        public abstract void AddOutputColumns(Table table);

        /// <summary>
        /// Runs the mapping for the specified row
        /// </summary>
        /// <param name="row"></param>
        /// <param name="joinRow"></param>
        /// <returns>0 filters or joins match, -1 row lessthan joinRow, 1 row greater than joinRow--></returns>
        public abstract bool ProcessInputRow(object[] row, object[] joinRow = null);

        /// <summary>
        /// Gets the mapping result, and updates the row.
        /// </summary>
        /// <param name="row">The output row to populate</param>
        public abstract void ProcessOutputRow(object[] row);

        /// <summary>
        /// Runs any aggregate functions, and populates the aggregate results.
        /// </summary>
        /// <param name="index">The row within the current group for aggregate functions.</param>
        /// <param name="row">The output row to populate</param>
        public abstract void ProcessResultRow(int index, object[] row);

        public abstract object GetInputValue(object[] row = null);

        /// <summary>
        /// Run a reset (if needed) of the mapping.
        /// </summary>
        public abstract void Reset();
        
        public int AddOutputColumn(Table table, TableColumn column)
        {
            var ordinal = table.GetOrdinal(column);
            if (ordinal < 0)
            {
                table.Columns.Add(column);
                ordinal = table.Columns.Count - 1;
            }

            return ordinal;
        }
        
    }
}