using System;
using System.Collections.Generic;
using System.IO;

namespace Lis.Core.Lis
{
    public sealed class LisLogicalFileParser
    {
        public LisLogicalFileData Parse(Stream stream, LisLogicalFile logicalFile)
        {
            return Parse(stream, logicalFile, options: null, metrics: null);
        }

        public LisLogicalFileData Parse(
            Stream stream,
            LisLogicalFile logicalFile,
            LisReadOptions? options,
            LisReadMetrics? metrics = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (logicalFile == null)
            {
                throw new ArgumentNullException(nameof(logicalFile));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(stream));
            }

            options = options ?? new LisReadOptions();
            HashSet<string>? selectedCurves = options.BuildSelectedCurveSet();

            var reader = new LisReader();
            var fixedParser = new LisFixedRecordParser();
            var dfsrParser = new LisDfsrParser();
            var fdataParser = new LisFdataParser();

            LisFileHeaderRecord? fileHeader = null;
            LisFileTrailerRecord? fileTrailer = null;
            LisDataFormatSpecificationRecord? activeDfsr = null;
            var dfsrs = new List<LisDataFormatSpecificationRecord>();
            var frames = new List<LisFrameData>(options.IncludeFrames ? logicalFile.Records.Count : 0);
            var textRecords = new List<LisTextRecord>();
            var curveAccumulator = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < logicalFile.Records.Count; i++)
            {
                LisRecordInfo info = logicalFile.Records[i];
                stream.Position = info.Offset;
                LisLogicalRecord record = reader.ReadNextLogicalRecord(stream);
                metrics?.AddLogicalRecordsRead(1);
                LisRecordType type = (LisRecordType)record.Header.Type;

                switch (type)
                {
                    case LisRecordType.FileHeader:
                        fileHeader = fixedParser.ParseFileHeader(record);
                        break;

                    case LisRecordType.FileTrailer:
                        fileTrailer = fixedParser.ParseFileTrailer(record);
                        break;

                    case LisRecordType.DataFormatSpecification:
                        activeDfsr = dfsrParser.Parse(record);
                        dfsrs.Add(activeDfsr);
                        break;

                    case LisRecordType.NormalData:
                    case LisRecordType.AlternateData:
                        if (activeDfsr == null)
                        {
                            throw new LisParseException(
                                "Encountered FData record before Data Format Specification Record.");
                        }

                        metrics?.AddFdataBytesRead(record.Data.Length);

                        if (options.IncludeFrames)
                        {
                            IReadOnlyList<LisFrameData> parsedFrames =
                                fdataParser.ParseFrames(record, activeDfsr, selectedCurves, metrics);
                            for (int frame = 0; frame < parsedFrames.Count; frame++)
                            {
                                frames.Add(parsedFrames[frame]);
                            }
                        }

                        if (options.IncludeCurves)
                        {
                            fdataParser.AccumulateCurves(record, activeDfsr, curveAccumulator, selectedCurves, metrics);
                        }

                        break;

                    case LisRecordType.OperatorCommandInputs:
                    case LisRecordType.OperatorResponseInputs:
                    case LisRecordType.SystemOutputs:
                    case LisRecordType.FlicComment:
                        textRecords.Add(fixedParser.ParseTextRecord(record));
                        break;
                }
            }

            var curves = new Dictionary<string, IReadOnlyList<object>>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, List<object>> item in curveAccumulator)
            {
                curves[item.Key] = item.Value;
            }

            return new LisLogicalFileData(fileHeader, fileTrailer, textRecords, dfsrs, frames, curves);
        }
    }
}
