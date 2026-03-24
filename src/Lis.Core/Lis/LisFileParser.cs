using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Точка входа высокого уровня для разбора всего LIS-потока на логические файлы.
    /// </summary>
    public sealed class LisFileParser
    {
        /// <summary>
        /// Выполняет полный разбор логических файлов с настройками по умолчанию.
        /// </summary>
        public IReadOnlyList<LisLogicalFileData> Parse(Stream stream)
        {
            return Parse(stream, options: null, metrics: null);
        }

        /// <summary>
        /// Разбирает LIS-поток с настраиваемой материализацией и сбором метрик.
        /// </summary>
        public IReadOnlyList<LisLogicalFileData> Parse(
            Stream stream,
            LisReadOptions? options,
            LisReadMetrics? metrics = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
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
            var stopwatch = Stopwatch.StartNew();
            // Сохраняем позицию вызывающего кода: после разбора она будет восстановлена.
            long originalPosition = stream.Position;
            stream.Position = 0;
            try
            {
                var indexer = new LisIndexer();
                LisRecordIndex index = indexer.Index(stream, options.AllowMalformedData, metrics);

                var partitioner = new LisLogicalFilePartitioner();
                IReadOnlyList<LisLogicalFile> logicalFiles = partitioner.Partition(index);
                if (options.AllowMalformedData && logicalFiles.Count == 0 && index.Count > 0)
                {
                    logicalFiles = new[]
                    {
                        new LisLogicalFile(index.Records, fileHeader: null, fileTrailer: null, isComplete: false)
                    };
                }

                var parser = new LisLogicalFileParser();
                var parsed = new List<LisLogicalFileData>(logicalFiles.Count);

                for (int i = 0; i < logicalFiles.Count; i++)
                {
                    parsed.Add(parser.Parse(stream, logicalFiles[i], options, metrics));
                }

                return parsed;
            }
            finally
            {
                stopwatch.Stop();
                metrics?.SetParseElapsedMilliseconds(stopwatch.ElapsedMilliseconds);
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Упрощённый режим извлечения кривых без материализации кадров.
        /// </summary>
        public IReadOnlyList<LisLogicalFileData> ParseCurves(
            Stream stream,
            IReadOnlyCollection<string>? selectedCurveMnemonics = null,
            LisReadMetrics? metrics = null)
        {
            var options = new LisReadOptions(
                selectedCurveMnemonics: selectedCurveMnemonics,
                includeFrames: false,
                includeCurves: true);

            return Parse(stream, options, metrics);
        }
    }
}
