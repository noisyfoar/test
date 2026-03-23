using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Lis.Core.Lis
{
    /// <summary>
    /// High-level entry point that parses a whole LIS stream into logical files.
    /// </summary>
    public sealed class LisFileParser
    {
        /// <summary>
        /// Parses full logical file data with default options.
        /// </summary>
        public IReadOnlyList<LisLogicalFileData> Parse(Stream stream)
        {
            return Parse(stream, options: null, metrics: null);
        }

        /// <summary>
        /// Parses LIS stream with optional materialization settings and metrics.
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
            // Preserve caller state: parser always restores original position.
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

        /// <summary>
        /// Convenience mode for curve extraction without frame materialization.
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
