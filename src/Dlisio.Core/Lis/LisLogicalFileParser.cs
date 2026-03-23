using System;
using System.Collections.Generic;
using System.IO;

namespace Dlisio.Core.Lis
{
    public sealed class LisLogicalFileParser
    {
        public LisLogicalFileData Parse(Stream stream, LisLogicalFile logicalFile)
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

            var reader = new LisReader();
            var fixedParser = new LisFixedRecordParser();
            var dfsrParser = new LisDfsrParser();
            var fdataParser = new LisFdataParser();

            LisFileHeaderRecord? fileHeader = null;
            LisFileTrailerRecord? fileTrailer = null;
            LisDataFormatSpecificationRecord? activeDfsr = null;
            var dfsrs = new List<LisDataFormatSpecificationRecord>();
            var frames = new List<LisFrameData>();
            var textRecords = new List<LisTextRecord>();

            for (int i = 0; i < logicalFile.Records.Count; i++)
            {
                LisRecordInfo info = logicalFile.Records[i];
                stream.Position = info.Offset;
                LisLogicalRecord record = reader.ReadNextLogicalRecord(stream);
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

                        IReadOnlyList<LisFrameData> parsedFrames = fdataParser.ParseFrames(record, activeDfsr);
                        for (int frame = 0; frame < parsedFrames.Count; frame++)
                        {
                            frames.Add(parsedFrames[frame]);
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

            return new LisLogicalFileData(fileHeader, fileTrailer, textRecords, dfsrs, frames);
        }
    }
}
