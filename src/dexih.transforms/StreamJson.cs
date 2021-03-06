﻿﻿using System;
 using System.Collections.Generic;
 using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
 using dexih.functions;
 using Newtonsoft.Json;

namespace dexih.transforms
{
    

    /// <summary>
    /// Writes out a simple json stream containing the headers and data for the reader.
    /// </summary>
    public class StreamJson : Stream
    {
        private const int BufferSize = 50000;
        private readonly DbDataReader _reader;
        private readonly MemoryStream _memoryStream;
        private readonly StreamWriter _streamWriter;
        private long _position;
        private readonly long _maxRows;
        private long _rowCount;
        private bool _hasRows;
        private bool _first;
        private readonly object[] _valuesArray;

        public StreamJson(string name, DbDataReader reader, long maxRows = -1)
        {
            _reader = reader;
            _memoryStream = new MemoryStream(BufferSize);
            _streamWriter = new StreamWriter(_memoryStream) {AutoFlush = true};
            _position = 0;

            _maxRows = maxRows <= 0 ? long.MaxValue : maxRows;
            _rowCount = 0;
            _hasRows = true;
            _first = true;

            _streamWriter.Write("{\"name\": \"" + System.Web.HttpUtility.JavaScriptStringEncode(name) + "\"");

            _streamWriter.Write(", \"columns\": ");

            // if this is a transform, then use the dataTypes from the cache table
            if (reader is Transform transform)
            {
                object ColumnObject(IEnumerable<TableColumn> columns)
                {
                    return columns?.Select(c => new {name = c.Name, logicalName = c.LogicalName, dataType = c.DataType, childColumns = ColumnObject(c.ChildColumns)});
                }
                
                var columnSerializeObject = JsonConvert.SerializeObject(ColumnObject( transform.CacheTable.Columns));
                _streamWriter.Write(columnSerializeObject);
            }
            else
            {
                for (var j = 0; j < reader.FieldCount; j++)
                {
                    var colName = reader.GetName(j);
                    _streamWriter.Write(JsonConvert.SerializeObject(new {name = colName, logicalName = colName, dataType = reader.GetDataTypeName(j)}) + ",");
                }
            }
            
            _valuesArray = new object[reader.FieldCount];

            _streamWriter.Write(", \"data\": [");
            _memoryStream.Position = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => -1;

        public override long Position { get => _position; set => throw new NotSupportedException("The position cannot be set."); }

        public override void Flush()
        {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if(!(_hasRows || _rowCount > _maxRows) && _memoryStream.Position >= _memoryStream.Length)
            {
                _reader.Close();
                return 0;
            }

            var readCount = await _memoryStream.ReadAsync(buffer, offset, count, cancellationToken);

            // if the buffer already has enough content.
            if (readCount < count && count > _memoryStream.Length - _memoryStream.Position)
            {
                _memoryStream.SetLength(0);

                if (_first)
                {
                    _hasRows = await _reader.ReadAsync(cancellationToken);
                    if (_hasRows == false)
                    {
                        await _streamWriter.WriteAsync("]}");
                    }
                    _first = false;
                }

                // populate the stream with rows, up to the buffer size.
                while (_hasRows)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _reader.Close();
                        return 0;
                    }

                    _reader.GetValues(_valuesArray);

                    for(var i = 0; i < _valuesArray.Length; i++)
                    {
                        if (_valuesArray[i] is byte[])
                            _valuesArray[i] = "binary data not available.";
                    }

                    var row = JsonConvert.SerializeObject(_valuesArray);

                    await _streamWriter.WriteAsync(row);

                    _rowCount++;
                    _hasRows = await _reader.ReadAsync(cancellationToken);

                    if (_hasRows && _rowCount < _maxRows)
                    {
                        await _streamWriter.WriteAsync(",");
                    }
                    else
                    {
                        await _streamWriter.WriteAsync("]}");
                        _hasRows = false;
                        break;
                    }
                }

                _memoryStream.Position = 0;

                readCount += await _memoryStream.ReadAsync(buffer, readCount, count - readCount, cancellationToken);
            }

            _position += readCount;

            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("The Seek function is not supported.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("The SetLength function is not supported.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("The Write function is not supported.");
        }
        
        public override void Close()
        {
            _streamWriter?.Close();
            _memoryStream?.Close();
            _reader?.Close();
            base.Close();
        }
    }
    
}
