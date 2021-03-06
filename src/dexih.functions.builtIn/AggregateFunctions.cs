﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dexih.functions.Query;
using Dexih.Utils.DataType;

namespace dexih.functions.BuiltIn
{
    public class AggregateFunctions<T>
    {
        private T OneHundred;

        private const string NullPlaceHolder = "A096F007-26EE-479E-A9E1-4E12427A5AF0"; //used a a unique string that can be substituted for null
        
        //The cache parameters are used by the functions to maintain a state during a transform process.
        // private int? _cacheInt;
        private double _cacheDouble;
        private DateTime? _cacheDate;
        private Dictionary<object, T> _cacheDictionary;
        private List<T> _cacheList;
        private List<double> _doubleList;
        private StringBuilder _cacheStringBuilder;
        private object _cacheObject;
        private object[] _cacheArray;
        private SortedRowsDictionary<T> _sortedRowsDictionary = null;
        private T _cacheNumber;
        private int _cacheCount;
        private HashSet<T> _hashSet;

        private bool _isFirst = true;

        public bool Reset()
        {
            _isFirst = true;
            // _cacheInt = null;
            _cacheDouble = 0;
            _cacheDate = null;
            _cacheDictionary = null;
            _cacheList = null;
            _cacheDictionary = null;
            _cacheStringBuilder = null;
            _cacheObject = null;
            _cacheArray = null;
            _sortedRowsDictionary = null;
            _cacheNumber = default(T);
            _cacheCount = 0;
            _doubleList = null;
            _hashSet = null;
            return true;
        }
        
        public DateTime? DateResult() => _cacheDate;
        public object ObjectResult() => _cacheObject;
        public T NumberResult() => _cacheNumber;
        public int CountResult() => _cacheCount;


        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Sum", Description = "Sum of the values", ResultMethod = nameof(NumberResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void Sum(T value) => _cacheNumber = Operations.Add<T>(_cacheNumber, value);

       
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Average", Description = "Average of the values", ResultMethod = nameof(AverageResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void Average(T value)
        {
            _cacheNumber = Operations.Add<T>(_cacheNumber, value);
            _cacheCount = _cacheCount + 1;
        }
        public T AverageResult()
        {
            if (_cacheCount == 0)
                return default(T);

            var type = typeof(T);

            return Operations.DivideInt<T>(_cacheNumber, _cacheCount);
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Minimum", Description = "Minimum Value", ResultMethod = nameof(NumberResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void Min(T value)
        {
            if (_isFirst)
            {
                _isFirst = false;
                _cacheNumber = value;
            }
            else if (Operations.LessThan<T>(value, _cacheNumber)) _cacheNumber = value;
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Maximum", Description = "Maximum Value", ResultMethod = nameof(NumberResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void Max(T value)
        {
            if (_isFirst)
            {
                _isFirst = false;
                _cacheNumber = value;
            }
            else if (Operations.GreaterThan<T>(value, _cacheNumber)) _cacheNumber = value;
        }

        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "First", Description = "First Value", ResultMethod = nameof(NumberResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void First(T value)
        {
            if (_isFirst)
            {
                _isFirst = false;
                _cacheNumber = value;
            }
        }

        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Last", Description = "Last Value", ResultMethod = nameof(NumberResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void Last(T value)
        {
            _cacheNumber = value;
        }

        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Count", Description = "Number of records", ResultMethod = nameof(CountResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public void Count()
        {
            _cacheCount++;
        }

       
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "CountTrue", Description = "Count where the value is true", ResultMethod = nameof(CountResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public void CountTrue(bool value)
        {
            
            if (value)
            {
                _cacheCount++;
            }
        }

        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "CountFalse", Description = "Count where the value is false", ResultMethod = nameof(CountResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public void CountFalse(bool value)
        {
            
            if (!value)
            {
                _cacheCount++;
            }
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "CountEqual", Description = "Count where the values are equal", ResultMethod = nameof(CountResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public void CountEqual(object[] values)
        {
            
            for (var i = 1; i < values.Length; i++)
            {
                if (!Equals(values[0], values[i])) return;
            }

            _cacheCount++;
        }


        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Count Distinct", Description = "Number if distinct values", ResultMethod = nameof(CountDistinctResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public void CountDistinct(T value)
        {
            if (_hashSet == null) _hashSet = new HashSet<T>();
            // if (value == null) value = NullPlaceHolder; //dictionary can't use nulls, so substitute null values.
            _hashSet.Add(value);
        }
        
        public int CountDistinctResult()
        {
            return _hashSet.Count;
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Concatenate Aggregate", Description = "Returns concatenated string of repeating values.", ResultMethod = nameof(ConcatAggResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public void ConcatAgg(string value, string delimiter)
        {
            if (_cacheStringBuilder == null)
            {
                _cacheStringBuilder = new StringBuilder();
                _cacheStringBuilder.Append(value);
            }
            else
                _cacheStringBuilder.Append(delimiter + value);
        }
        
        public string ConcatAggResult()
        {
            return _cacheStringBuilder.ToString();
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Median", Description = "The median value in a series", ResultMethod = nameof(MedianResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void Median(T value)
        {
            if (_cacheList == null) _cacheList = new List<T>();
            _cacheList.Add(value);
        }
        public T MedianResult()
        {
            if (_cacheList == null)
                return default(T);
            var sorted = _cacheList.OrderBy(c => c).ToArray();
            var count = sorted.Length;

            var type = typeof(T);

            if (count % 2 == 0)
            {
                // count is even, average two middle elements
                var a = sorted[count / 2 - 1];
                var b = sorted[count / 2];
                return Operations.DivideInt<T>(Operations.Add<T>(a, b), 2);
            }
            // count is odd, return the middle element
            return sorted[count / 2];
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Standard Deviation", Description = "The standard deviation in a set of numbers", ResultMethod = nameof(StdDevResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void StdDev(double value)
        {
            Variance(value);
        }

        public double StdDevResult()
        {
            var sd = Math.Sqrt(VarianceResult());
            return sd;
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Variance", Description = "The variance in a set of numbers.", ResultMethod = nameof(VarianceResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void Variance(double value)
        {
            if (_doubleList == null) _doubleList = new List<double>();

            _doubleList.Add(value);
            _cacheCount++;
            _cacheDouble += value;
        }

        public double VarianceResult()
        {
            if (_doubleList == null || _cacheCount == 0 )
                return 0;

            var average = _cacheDouble / _cacheCount;
            var sumOfSquaresOfDifferences = _doubleList.Select(val => (val - average) * (val - average)).Sum();
            var sd = sumOfSquaresOfDifferences / _cacheCount;

            return sd;
        }
        
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "First When", Description = "First resultValue when the condition = conditionValue", ResultMethod = nameof(ObjectResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void FirstWhen(object condition, object conditionValue, T resultValue)
        {
            if (Equals(condition, conditionValue) && _cacheObject == null)
            {
                _cacheObject = resultValue;
            }
        }

        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Last When", Description = "Last resultValue when the condition = conditionValue", ResultMethod = nameof(ObjectResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void LastWhen(object condition, object conditionValue, T resultValue)
        {
            if (Equals(condition, conditionValue))
            {
                _cacheObject = resultValue;
            }
        }

        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Pivot to Columns", 
            Description = "Pivots the labelColumn and valueColumn into separate columns specified by the labels.  Returns true if all labels are found, false is some are missing.", ResultMethod = nameof(PivotToColumnsResult), ResetMethod = nameof(Reset), GenericType = EGenericType.All)]
        public void PivotToColumns(string labelColumn, T valueColumn, [TransformFunctionParameterTwin] object[] labels)
        {
            if (_cacheDictionary == null)
            {
                _cacheDictionary = new Dictionary<object, T>();
                foreach (var label in labels)
                {
                    _cacheDictionary.Add(label, default(T));
                }
            }

            if (_cacheDictionary.ContainsKey(labelColumn))
            {
                _cacheDictionary[labelColumn] = valueColumn;
            }
        }
        
        public bool PivotToColumnsResult([TransformFunctionParameterTwin] out T[] values)
        {
            values = _cacheDictionary.Values.ToArray();
            return !values.Contains(default(T));
        }
        
        public enum EPercentFormat
        {
            AsDecimal,
            AsPercent
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Percent of Total", Description = "The percentage total in the group.", ResultMethod = nameof(PercentTotalResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void PercentTotal(T value)
        {
            if (_cacheList == null)
            {
                _cacheList = new List<T>();
                _cacheNumber = default(T);
                OneHundred = Operations.Parse<T>(100);
            }
            _cacheNumber = Operations.Add<T>(_cacheNumber, value);
            _cacheList.Add(value);        
        }

        public T PercentTotalResult([TransformFunctionVariable(EFunctionVariable.Index)]int index, EPercentFormat percentFormat = EPercentFormat.AsPercent)
        {
            if (_cacheList == null || Operations.Equal<T>(_cacheNumber, default(T)))
                return default(T);

            var percent = Operations.Divide<T>(_cacheList[index], _cacheNumber);
            
            return percentFormat == EPercentFormat.AsDecimal ? percent : Operations.Multiply<T>(percent, OneHundred);
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Rank", Description = "The ranking (starting at 1) of the item within the group", ResultMethod = nameof(RankResult), ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public void Rank(T[] values)
        {
            if (_sortedRowsDictionary == null)
            {
                _sortedRowsDictionary = new SortedRowsDictionary<T>();
                _cacheCount = 0;
            }

            if (_sortedRowsDictionary.ContainsKey(values))
            {
                var indexes = _sortedRowsDictionary[values];
                Array.Resize(ref indexes, indexes.Length + 1);
                indexes[indexes.Length-1] = _cacheCount;
                _sortedRowsDictionary[values] = indexes;
            }
            else
            {
                _sortedRowsDictionary.Add(values, new object[] {_cacheCount});    
            }

            _cacheCount++;
        }

        public int RankResult([TransformFunctionVariable(EFunctionVariable.Index)]int index, Sort.EDirection sortDirection)
        {
            if (_cacheArray == null)
            {
                _cacheArray = new object[_cacheCount];
                int rank;
                int increment;
                if (sortDirection == Sort.EDirection.Ascending)
                {
                    rank = 1;
                    increment = 1;
                }
                else
                {
                    rank = _sortedRowsDictionary.Count();
                    increment = -1;
                }
                
                foreach (var item in _sortedRowsDictionary)
                {
                    foreach (var value in item.Value)
                    {
                        _cacheArray[(int) value] = rank;    
                    }
                    
                    rank += increment * item.Value.Length;
                }
            }

            return (int)_cacheArray[index];
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Running Count", Description = "The running count of rows in the current group.", ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Int32, GenericType = EGenericType.Numeric)]
        public int RunningCount()
        {
            _cacheCount++;
            return _cacheCount;
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Running Sum", Description = "The running sum of rows in the current group.", ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public object RunningSum(T value)
        {
            _cacheNumber = Operations.Add(_cacheNumber, value);
            return _cacheNumber;
        }
        
        [TransformFunction(FunctionType = EFunctionType.Aggregate, Category = "Aggregate", Name = "Running Average", Description = "The running average of rows in the current group.", ResetMethod = nameof(Reset), GenericTypeDefault = DataType.ETypeCode.Decimal, GenericType = EGenericType.Numeric)]
        public object RunningAverage(T value)
        {
            _cacheNumber = Operations.Add(_cacheNumber, value);
            _cacheCount++;
            return Operations.DivideInt(_cacheNumber, _cacheCount);
        }
        

    }
}