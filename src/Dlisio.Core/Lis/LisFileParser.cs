using System;
using System.Collections.Generic;
using System.IO;

namespace Dlisio.Core.Lis
{
    public sealed class LisFileParser
    {
        public IReadOnlyList<LisLogicalFileData> Parse(Stream stream)
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
                    parsed.Add(parser.Parse(stream, logicalFiles[i]));
                }

                return parsed;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }
    }
}
