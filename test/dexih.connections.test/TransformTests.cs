﻿using dexih.connections;
using dexih.functions;
using dexih.functions.Query;
using dexih.transforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace dexih.connections.test
{
    public class TransformTests
    {

        public async Task Transform(Connection connection, string databaseName)
        {
            var table = DataSets.CreateTable();

            await connection.CreateDatabase(databaseName, CancellationToken.None);

            //create a new table and write some data to it.  
            Transform reader = DataSets.CreateTestData();
            await connection.CreateTable(table, true, CancellationToken.None);
            var writer = new TransformWriterBulk();

            var writerResult = new TransformWriterResult();
            await connection.InitializeAudit(writerResult, 0, 1, "DataLink", 1, 2, "Test", 1, "Source", 2, "Target", TransformWriterResult.ETriggerMethod.Manual, "Test", CancellationToken.None);
            var writeRecords = await writer.WriteAllRecords(writerResult, reader, table, connection, null, null, null, null, CancellationToken.None);

            Assert.True(writeRecords, $"WriteAllRecords failed with message {writerResult.Message}.  Details:{writerResult.ExceptionDetails}");

            //check database can sort 
            if (connection.CanSort)
            {
                //use the new table test the data base is sorting
                reader = connection.GetTransformReader(table);

                var query = new SelectQuery()
                {
                    Sorts = new List<Sort>() { new Sort("IntColumn", Sort.EDirection.Descending) }
                };
                await reader.Open(0, query, CancellationToken.None);


                var sortValue = 10;
                while (await reader.ReadAsync())
                {
                    Assert.Equal(sortValue, Convert.ToInt32(reader["IntColumn"]));
                    sortValue--;
                }
                Assert.Equal(0, sortValue);
            }

            //check database can filter
            if (connection.CanFilter)
            {
                //use the new table to test database is filtering
                reader = connection.GetTransformReader(table);

                var query = new SelectQuery()
                {
                    Filters = new List<Filter>() { new Filter("IntColumn", Filter.ECompare.LessThanEqual, 5) }
                };
                await reader.Open(0, query, CancellationToken.None);


                var count = 0;
                while (await reader.ReadAsync())
                {
                    Assert.True(Convert.ToInt32(reader["IntColumn"]) <= 5);
                    count++;
                }
                Assert.Equal(5, count);
            }

            var deltaTable = DataSets.CreateTable();
            deltaTable.AddAuditColumns();
            deltaTable.Name = "DeltaTable";
            await connection.CreateTable(deltaTable, true, CancellationToken.None);

            var targetReader = connection.GetTransformReader(deltaTable);
            reader = connection.GetTransformReader(table);
            var transformDelta = new TransformDelta(reader, targetReader, TransformDelta.EUpdateStrategy.AppendUpdate, 1, false);
            await transformDelta.Open(0, null, CancellationToken.None);

            writerResult = new TransformWriterResult();
            await connection.InitializeAudit(writerResult, 0, 1, "Datalink", 1, 2, "Test", 1, "Source", 2, "Target", TransformWriterResult.ETriggerMethod.Manual, "Test", CancellationToken.None);

            var writeAllResult = await writer.WriteAllRecords(writerResult, transformDelta, deltaTable, connection, CancellationToken.None);
            Assert.True(writeAllResult, writerResult.Message);
            Assert.Equal(10L, writerResult.RowsCreated);

            //check the audit table loaded correctly.
            var auditTable = await connection.GetTransformWriterResults(0, 1, null, "Datalink", writerResult.AuditKey, null, true, false, false, null, 1, null, false, CancellationToken.None);
            Assert.Equal(10L, auditTable[0].RowsCreated);
        }

    }
}
