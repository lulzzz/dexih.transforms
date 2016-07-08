﻿using dexih.transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dexih.functions;
using static dexih.functions.DataType;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace dexih.connections.webservice
{
    public class ReaderRestful : Transform
    {
        private bool _isOpen = false;

        public ReaderRestful(Connection connection, Table table, Transform referenceTransform)
        {
            ReferenceConnection = connection;
            CacheTable = table;
            ReferenceTransform = referenceTransform;
        }

        public override async Task<ReturnValue> Open(SelectQuery query)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_isOpen)
                    {
                        return new ReturnValue(false, "The web service connection is already open.", null);
                    }

                    //if no driving table is set, then use the row creator to simulate a single row.
                    if (ReferenceTransform == null)
                    {
                        ReaderRowCreator rowCreator = new ReaderRowCreator();
                        rowCreator.InitializeRowCreator(1, 1, 1);
                        base.ReferenceTransform = rowCreator;
                    }

                    //create a dummy inreader to allow fieldcount and other queries to work.
                    return new ReturnValue(true);
                }
                catch (Exception ex)
                {
                    return new ReturnValue(false, "The following error occurred when starting the web service: " + ex.Message, ex);
                }
            });
        }

        public override string Details()
        {
            return "Restful Service";
        }

        public override bool InitializeOutputFields()
        {
            return true;
        }

        public override ReturnValue ResetTransform()
        {
            return new ReturnValue(true);
        }

        protected override async Task<ReturnValue<object[]>> ReadRecord(CancellationToken cancellationToken)
        {
            try
            {
                if (await ReferenceTransform.ReadAsync(cancellationToken) == false)
                    return new ReturnValue<object[]>(false, null);
                else
                {
                    List<Filter> filters = new List<Filter>();

                    foreach (JoinPair join in JoinPairs)
                    {
                        var joinValue = join.JoinColumn == null ? join.JoinValue : ReferenceTransform[join.JoinColumn].ToString();

                        filters.Add(new Filter()
                        {
                            Column1 = join.SourceColumn,
                            CompareDataType = ETypeCode.String,
                            Operator = Filter.ECompare.IsEqual,
                            Value2 = joinValue
                        });
                    }

                    var result = await LookupRow(filters);

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("The restful service failed due to the following error: " + ex.Message, ex);
            }
        }

        public override bool CanLookupRowDirect { get; } = true;

        /// <summary>
        /// This performns a lookup directly against the underlying data source, returns the result, and adds the result to cache.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public override async Task<ReturnValue<object[]>> LookupRowDirect(List<Filter> filters)
        {
            return await ((ConnectionRestful) ReferenceConnection).LookupRow(CacheTable, filters);
         }
    }
}