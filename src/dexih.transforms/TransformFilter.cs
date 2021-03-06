﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using dexih.functions.Query;
using dexih.transforms.Mapping;
using dexih.transforms.Transforms;

namespace dexih.transforms
{
    [Transform(
        Name = "Filter",
        Description = "Filter incoming rows based on a set of conditions.",
        TransformType = TransformAttribute.ETransformType.Filter
    )]
    public class TransformFilter : Transform
    {
        public TransformFilter() { }

        public TransformFilter(Transform inReader, Mappings mappings)
        {
            Mappings = mappings;
            SetInTransform(inReader);
        }
        
        public override bool RequiresSort => false;
       
        public override async Task<bool> Open(long auditKey, SelectQuery query, CancellationToken cancellationToken)
        {
            AuditKey = auditKey;

            if (query == null)
                query = new SelectQuery();

            if (query.Filters == null)
                query.Filters = new List<Filter>();

            //add any of the conditions that can be translated to filters
            foreach (var condition in Mappings.OfType<MapFunction>())
            {
                var filter = condition.GetFilterFromFunction();
                if (filter != null)
                {
                    filter.AndOr = Filter.EAndOr.And;
                    query.Filters.Add(filter);
                }
            }

            foreach (var filterPair in Mappings.OfType<MapFilter>())
            {
                if (filterPair.Column2 == null)
                {
                    var filter = new Filter(filterPair.Column1, filterPair.Compare, filterPair.Value2);
                    query.Filters.Add(filter);
                }
                else
                {
                    var filter = new Filter(filterPair.Column1, filterPair.Compare, filterPair.Column2);
                    query.Filters.Add(filter);
                }
            }
            
            var returnValue = await PrimaryTransform.Open(auditKey, query, cancellationToken);

            return returnValue;
        }

        protected override async Task<object[]> ReadRecord(CancellationToken cancellationToken)
        {
            if (!await PrimaryTransform.ReadAsync(cancellationToken))
            {
                return null;
            }

            var showRecord = true;
            
            if (Mappings != null && ( Mappings.OfType<MapFunction>().Any() || Mappings.OfType<MapFilter>().Any()))
            {
                do //loop through the records util the filter is true
                {
                    showRecord = await Mappings.ProcessInputData(PrimaryTransform.CurrentRow);

                    if (showRecord) break;

                    TransformRowsFiltered += 1;
                } while (await PrimaryTransform.ReadAsync(cancellationToken));
            }

            object[] newRow;

            if (showRecord)
            {
                newRow = PrimaryTransform.CurrentRow;
            }
            else
            {
                newRow = null;
            }

            return newRow;
        }

        public override bool ResetTransform()
        {
            return true;
        }

        public override string Details()
        {
            return "Filter: Number of filters= " + Mappings?.OfType<MapFilter>().Count() + ", functions= " + Mappings?.OfType<MapFunction>();
        }

        public override List<Sort> RequiredSortFields()
        {
            return null;
        }

        public override List<Sort> RequiredReferenceSortFields()
        {
            return null;
        }

    }
}
