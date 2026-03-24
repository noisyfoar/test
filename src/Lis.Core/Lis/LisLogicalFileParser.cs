using System;
using System.Collections.Generic;
using System.IO;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Разбирает область одного логического LIS-файла в типизированные объекты.
    /// </summary>
    public sealed class LisLogicalFileParser
    {
        /// <summary>
        /// Разбирает логический файл с настройками по умолчанию.
        /// </summary>
        public LisLogicalFileData Parse(Stream stream, LisLogicalFile logicalFile)
        {
            return Parse(stream, logicalFile, options: null, metrics: null);
        }

        /// <summary>
        /// Разбирает логический файл с выборочной материализацией данных.
        /// </summary>
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
                LisLogicalRecord record;
                try
                {
                    record = reader.ReadNextLogicalRecord(stream);
                }
                catch (Exception) when (options.AllowMalformedData)
                {
                    metrics?.AddMalformedRecordsSkipped(1);
                    continue;
                }

                metrics?.AddLogicalRecordsRead(1);
                LisRecordType type = (LisRecordType)record.Header.Type;

                switch (type)
                {
                    case LisRecordType.FileHeader:
                        try
                        {
                            fileHeader = fixedParser.ParseFileHeader(record);
                        }
                        catch (Exception) when (options.AllowMalformedData)
                        {
                            metrics?.AddMalformedRecordsSkipped(1);
                        }

                        break;

                    case LisRecordType.FileTrailer:
                        try
                        {
                            fileTrailer = fixedParser.ParseFileTrailer(record);
                        }
                        catch (Exception) when (options.AllowMalformedData)
                        {
                            metrics?.AddMalformedRecordsSkipped(1);
                        }

                        break;

                    case LisRecordType.DataFormatSpecification:
                        try
                        {
                            activeDfsr = dfsrParser.Parse(record);
                            dfsrs.Add(activeDfsr);
                        }
                        catch (Exception) when (options.AllowMalformedData)
                        {
                            metrics?.AddMalformedRecordsSkipped(1);
                        }

                        break;

                    case LisRecordType.NormalData:
                    case LisRecordType.AlternateData:
                        try
                        {
                            HandleDataRecord(
                                record,
                                activeDfsr,
                                options,
                                selectedCurves,
                                metrics,
                                fdataParser,
                                frames,
                                curveAccumulator);
                        }
                        catch (Exception) when (options.AllowMalformedData)
                        {
                            metrics?.AddMalformedRecordsSkipped(1);
                        }

                        break;

                    case LisRecordType.OperatorCommandInputs:
                    case LisRecordType.OperatorResponseInputs:
                    case LisRecordType.SystemOutputs:
                    case LisRecordType.FlicComment:
                        try
                        {
                            textRecords.Add(fixedParser.ParseTextRecord(record));
                        }
                        catch (Exception) when (options.AllowMalformedData)
                        {
                            metrics?.AddMalformedRecordsSkipped(1);
                        }

                        break;
                }
            }

            return new LisLogicalFileData(
                fileHeader,
                fileTrailer,
                textRecords,
                dfsrs,
                frames,
                BuildReadonlyCurves(curveAccumulator));
        }

        /// <summary>
        /// Подробно выполняет операцию «HandleDataRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void HandleDataRecord(
            LisLogicalRecord record,
            LisDataFormatSpecificationRecord? activeDfsr,
            LisReadOptions options,
            HashSet<string>? selectedCurves,
            LisReadMetrics? metrics,
            LisFdataParser fdataParser,
            List<LisFrameData> frames,
            Dictionary<string, List<object>> curveAccumulator)
        {
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
        }

        /// <summary>
        /// Подробно выполняет операцию «BuildReadonlyCurves» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static IReadOnlyDictionary<string, IReadOnlyList<object>> BuildReadonlyCurves(
            Dictionary<string, List<object>> curveAccumulator)
        {
            var curves = new Dictionary<string, IReadOnlyList<object>>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, List<object>> item in curveAccumulator)
            {
                curves[item.Key] = item.Value;
            }

            return curves;
        }
    }
}
