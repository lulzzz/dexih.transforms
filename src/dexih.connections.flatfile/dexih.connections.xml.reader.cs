﻿using dexih.transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dexih.functions;
using System.Data.Common;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace dexih.connections.flatfile
{
    public class ReaderFlatFile : Transform
    {
        private bool _isOpen = false;

        DexihFiles _files;
        CsvReader _csvReader;

        FileFormat _fileFormat;

        ConnectionFlatFile FileConnection;

		public FlatFile CacheFlatFile {
			get { return (FlatFile)CacheTable; }
		}

        public ReaderFlatFile(Connection connection, FlatFile table)
        {
            ReferenceConnection = connection;
            FileConnection = (ConnectionFlatFile)connection;
            CacheTable = table;
        }

        protected override void Dispose(bool disposing)
        {
            if (_csvReader != null)
                _csvReader.Dispose();

            _isOpen = false;

            base.Dispose(disposing);
        }

        public override async Task<ReturnValue> Open(Int64 auditKey, SelectQuery query)
        {
            AuditKey = auditKey;

            if (_isOpen)
            {
                return new ReturnValue(false, "The file reader connection is already open.", null);
            }

            var fileEnumerator = await ((ConnectionFlatFile)ReferenceConnection).GetFileEnumerator(CacheFlatFile.FileRootPath, CacheFlatFile.FileIncomingPath);
            if (fileEnumerator.Success == false)
                return fileEnumerator;

            _files = fileEnumerator.Value;

            if (_files.MoveNext() == false)
            {
                return new ReturnValue(false, "There are no files in the incomming directory.", null);
            }

            var fileStream = await ((ConnectionFlatFile)ReferenceConnection).GetReadFileStream(CacheFlatFile, CacheFlatFile.FileIncomingPath, _files.Current.FileName);
            if (fileStream.Success == false)
                return fileStream;

			//string fileFormatString = CacheTable.GetExtendedProperty("FileFormat");

			//if (string.IsNullOrEmpty(fileFormatString))
			//    _fileFormat = new FileFormat();
			//else
			//_fileFormat = JsonConvert.DeserializeObject<FileFormat>(CacheTable.GetExtendedProperty("FileFormat"));

			_fileFormat = CacheFlatFile.FileFormat;

            _csvReader = new CsvReader(new StreamReader(fileStream.Value), _fileFormat);

            return new ReturnValue(true);
        }

        public override string Details()
        {
            return "FlatFile";
        }

        public override bool InitializeOutputFields()
        {
            return true;
        }

        public override ReturnValue ResetTransform()
        {
            if (_isOpen)
            {
                return new ReturnValue(true);
            }
            else
                return new ReturnValue(false, "The flatfile reader can not be reset", null);

        }

        protected override async Task<ReturnValue<object[]>> ReadRecord(CancellationToken cancellationToken)
        {
            bool notfinished;
            try
            {
                notfinished = await _csvReader.ReadAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("The flatfile reader failed with the following message: " + ex.Message, ex);
            }

            if (notfinished == false)
            {
                _csvReader.CloseFile();

                var moveFileResult = await ((ConnectionFlatFile)ReferenceConnection).MoveFile(CacheFlatFile, _files.Current.FileName, CacheFlatFile.FileIncomingPath, CacheFlatFile.FileProcessedPath); //backup the completed file
                if (moveFileResult.Success == false)
                {
                    throw new Exception("The flatfile reader failed with the following message: " + moveFileResult.Message);
                }

                if (_files.MoveNext() == false)
                    _isOpen = false;
                else
                {
                    var fileStream = await ((ConnectionFlatFile)ReferenceConnection).GetReadFileStream(CacheFlatFile, CacheFlatFile.FileIncomingPath, _files.Current.FileName);
                    if (fileStream.Success == false)
                        throw new Exception("The flatfile reader failed with the following message: " + fileStream.Message);

                    _csvReader = new CsvReader(new StreamReader(fileStream.Value), _fileFormat);
                    try
                    {
                        notfinished = await _csvReader.ReadAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("The flatfile reader failed with the following message: " + ex.Message, ex);
                    }
                    if (notfinished == false)
                        return await ReadRecord(cancellationToken); // this creates a recurive loop to cater for empty files.
                }
            }

            if (notfinished)
            {
                object[] row = new object[CacheTable.Columns.Count];
                _csvReader.GetValues(row);
                return new ReturnValue<object[]>(true, row);
            }
            else
                return new ReturnValue<object[]>(false, null);

        }

        public override bool CanLookupRowDirect { get; } = false;

        /// <summary>
        /// This performns a lookup directly against the underlying data source, returns the result, and adds the result to cache.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public override Task<ReturnValue<object[]>> LookupRowDirect(List<Filter> filters)
        {
            throw new NotSupportedException("Lookup not supported with flat files.");
        }
    }
}