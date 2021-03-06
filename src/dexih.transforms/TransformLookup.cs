﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using dexih.functions.Query;
using dexih.transforms.Mapping;
using static Dexih.Utils.DataType.DataType;
using dexih.transforms.Transforms;

namespace dexih.transforms
{

    /// <summary>
    /// The join table is loaded into memory and then joined to the primary table.
    /// </summary>
    [Transform(
        Name = "Lookup",
        Description = "Looks up a value in a database or external service.",
        TransformType = TransformAttribute.ETransformType.Lookup
    )]
    public class TransformLookup : Transform
    {
        private int _primaryFieldCount;
        private int _referenceFieldCount;

        private IEnumerator<object[]> _lookupCache;

        public TransformLookup() { }

        public TransformLookup(Transform primaryTransform, Transform joinTransform, Mappings mappings, string referenceTableAlias)
        {
            Mappings = mappings;
            //JoinPairs = joinPairs;
            ReferenceTableAlias = referenceTableAlias;

            SetInTransform(primaryTransform, joinTransform);
        }

        public override async Task<bool> Open(long auditKey, SelectQuery query, CancellationToken cancellationToken)
        {
            _primaryFieldCount = PrimaryTransform.FieldCount;
            _referenceFieldCount = ReferenceTransform.FieldCount;

            ReferenceTransform.SetCacheMethod(ECacheMethod.LookupCache, 1000);
            
            var returnValue = await PrimaryTransform.Open(auditKey, query, cancellationToken);
            return returnValue;
        }


        public override bool RequiresSort => false;

        protected override async Task<object[]> ReadRecord(CancellationToken cancellationToken)
        {
            object[] newRow = null;

            // if there is a previous lookup cache, then just populated that as the next row.
            if(_lookupCache != null && _lookupCache.MoveNext())
            {
                newRow = new object[FieldCount];
                var pos1 = 0;
                for (var i = 0; i < _primaryFieldCount; i++)
                {
                    newRow[pos1] = PrimaryTransform[i];
                    pos1++;
                }

                var lookup = _lookupCache.Current;
                for (var i = 0; i < _referenceFieldCount; i++)
                {
                    newRow[pos1] = lookup[i];
                    pos1++;
                }

                return newRow;
            }

            _lookupCache = null;

            if (await PrimaryTransform.ReadAsync(cancellationToken)== false)
            {
                return null;
            }

            //load in the primary table values
            newRow = new object[FieldCount];
            var pos = 0;
            for (var i = 0; i < _primaryFieldCount; i++)
            {
                newRow[pos] = PrimaryTransform[i];
                pos++;
            }

            //set the values for the lookup
            await Mappings.ProcessInputData(PrimaryTransform.CurrentRow);
            
            // create a select query with filters set to the values of the current row
            var selectQuery = new SelectQuery();
            selectQuery.Filters = Mappings.OfType<MapJoin>().Select(c => new Filter()
            {
                Column1 = c.JoinColumn,
                CompareDataType = ETypeCode.String,
                Operator = Filter.ECompare.IsEqual,
                Value2 = c.GetOutputTransform()
            }).ToList();
            
            var lookupResult = await ReferenceTransform.Lookup(selectQuery, JoinDuplicateStrategy?? EDuplicateStrategy.Abend, cancellationToken);
            if (lookupResult != null)
            {
                _lookupCache = lookupResult.GetEnumerator();

                if (_lookupCache.MoveNext())
                {
                    var lookup = _lookupCache.Current;
                    for (var i = 0; i < _referenceFieldCount; i++)
                    {
                        newRow[pos] = lookup[i];
                        pos++;
                    }
                }
                else
                {
                    _lookupCache = null;
                }
            }
            else
            {
                _lookupCache = null;
            }

            return newRow;
        }

        public override bool ResetTransform()
        {
            return true;
        }

        public override string Details()
        {
            return "Lookup";
        }

        public override List<Sort> RequiredSortFields()
        {
            var fields = new List<Sort>();
            return fields;
        }

        public override List<Sort> RequiredReferenceSortFields()
        {
            return null;
        }
    }
}
