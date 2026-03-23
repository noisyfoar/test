using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Dlisio.Core.Lis
{
    public sealed class LisFileParser
    {
        public IReadOnlyList<LisLogicalFileData> Parse(Stream stream)
        {
            return Parse(stream, options: null, metrics: null);
        }

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
            long originalPosition = stream.Position;
            stream.Position = 0;
            try
            {
                var indexer = new LisIndexer();
                LisRecordIndex index = indexer.Index(stream);

                var partitioner = new LisLogicalFilePartitioner();
                IReadOnlyList<LisLogicalFile> logicalFiles = partitioner.Partition(index);

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
