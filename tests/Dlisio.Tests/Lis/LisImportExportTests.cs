using System;
using System.Collections.Generic;
using System.IO;
using Dlisio.Core.Lis;
using Xunit;

namespace Dlisio.Tests.Lis
{
    public sealed class LisImportExportTests
    {
        [Fact]
        public void Import_Stream_ReadsLogicalRecords()
        {
            byte[] first = BuildSinglePhysicalLogicalRecord(
                LisRecordType.FileHeader,
                logicalRecordAttributes: 0x11,
                data: new byte[] { 0xAA, 0xBB });

            byte[] second = BuildTwoPhysicalLogicalRecord(
                LisRecordType.NormalData,
                logicalRecordAttributes: 0x22,
                firstChunk: new byte[] { 0x01, 0x02, 0x03 },
                secondChunk: new byte[] { 0x04, 0x05 });

            using var stream = new MemoryStream(Concat(first, second));
            var importer = new LisImporter();

            LisDocument document = importer.Import(stream);

            Assert.Equal(2, document.Records.Count);
            Assert.Equal((byte)LisRecordType.FileHeader, document.Records[0].Header.Type);
            Assert.Equal((byte)0x11, document.Records[0].Header.Attributes);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, document.Records[0].Data);
            Assert.Equal(1, document.Records[0].PhysicalRecordCount);

            Assert.Equal((byte)LisRecordType.NormalData, document.Records[1].Header.Type);
            Assert.Equal((byte)0x22, document.Records[1].Header.Attributes);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, document.Records[1].Data);
            Assert.Equal(2, document.Records[1].PhysicalRecordCount);

            Assert.Equal(0L, stream.Position);
        }

        [Fact]
        public void Import_Path_ReadsLogicalRecords()
        {
            byte[] bytes = BuildSinglePhysicalLogicalRecord(
                LisRecordType.FileTrailer,
                logicalRecordAttributes: 0x01,
                data: new byte[] { 0x10 });

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".lis");
            try
            {
                File.WriteAllBytes(path, bytes);
                var importer = new LisImporter();

                LisDocument document = importer.Import(path);

                Assert.Single(document.Records);
                Assert.Equal((byte)LisRecordType.FileTrailer, document.Records[0].Header.Type);
                Assert.Equal(new byte[] { 0x10 }, document.Records[0].Data);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void Export_ThenImport_RoundTripsLogicalRecords()
        {
            var source = new LisDocument(new[]
            {
                new LisLogicalRecord(
                    new LisLogicalRecordHeader((byte)LisRecordType.DataFormatSpecification, 0x7A),
                    new byte[] { 0x01, 0x02, 0x03, 0x04 },
                    physicalRecordCount: 1),
                new LisLogicalRecord(
                    new LisLogicalRecordHeader((byte)LisRecordType.NormalData, 0x00),
                    new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 },
                    physicalRecordCount: 1)
            });

            using var stream = new MemoryStream();
            var exporter = new LisExporter();
            exporter.Export(stream, source);

            stream.Position = 0;
            var importer = new LisImporter();
            LisDocument imported = importer.Import(stream);

            Assert.Equal(source.Records.Count, imported.Records.Count);
            for (int i = 0; i < source.Records.Count; i++)
            {
                Assert.Equal(source.Records[i].Header.Type, imported.Records[i].Header.Type);
                Assert.Equal(source.Records[i].Header.Attributes, imported.Records[i].Header.Attributes);
                Assert.Equal(source.Records[i].Data, imported.Records[i].Data);
            }
        }

        [Fact]
        public void Export_WithMaxPhysicalRecordLength_SplitsRecordIntoMultiplePhysicalRecords()
        {
            var source = new LisDocument(new[]
            {
                new LisLogicalRecord(
                    new LisLogicalRecordHeader((byte)LisRecordType.NormalData, 0x00),
                    BuildSequence(20),
                    physicalRecordCount: 1)
            });

            using var stream = new MemoryStream();
            var exporter = new LisExporter();
            exporter.Export(stream, source, new LisExportOptions(maxPhysicalRecordLength: 12));

            stream.Position = 0;
            var importer = new LisImporter();
            LisDocument imported = importer.Import(stream);

            Assert.Single(imported.Records);
            Assert.Equal(3, imported.Records[0].PhysicalRecordCount);
            Assert.Equal(BuildSequence(20), imported.Records[0].Data);
        }

        [Fact]
        public void Export_InvalidMaxPhysicalRecordLength_ThrowsArgumentOutOfRangeException()
        {
            var source = new LisDocument(new[]
            {
                new LisLogicalRecord(
                    new LisLogicalRecordHeader((byte)LisRecordType.NormalData, 0x00),
                    new byte[] { 0x01 },
                    physicalRecordCount: 1)
            });

            using var stream = new MemoryStream();
            var exporter = new LisExporter();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => exporter.Export(stream, source, new LisExportOptions(maxPhysicalRecordLength: 6)));
        }

        [Fact]
        public void ImportExport_NullArguments_Throw()
        {
            var importer = new LisImporter();
            var exporter = new LisExporter();
            var document = new LisDocument(Array.Empty<LisLogicalRecord>());
            using var stream = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() => importer.Import((string)null!));
            Assert.Throws<ArgumentNullException>(() => importer.Import((Stream)null!));
            Assert.Throws<ArgumentNullException>(() => exporter.Export((string)null!, document));
            Assert.Throws<ArgumentNullException>(() => exporter.Export(stream, null!));
            Assert.Throws<ArgumentNullException>(() => exporter.Export((Stream)null!, document));
        }

        private static byte[] BuildSinglePhysicalLogicalRecord(
            LisRecordType type,
            byte logicalRecordAttributes,
            byte[] data)
        {
            byte[] payload = BuildLrhPayload((byte)type, logicalRecordAttributes, data);
            return BuildPhysicalRecord(0x0000, payload);
        }

        private static byte[] BuildTwoPhysicalLogicalRecord(
            LisRecordType type,
            byte logicalRecordAttributes,
            byte[] firstChunk,
            byte[] secondChunk)
        {
            byte[] firstPayload = BuildLrhPayload((byte)type, logicalRecordAttributes, firstChunk);
            byte[] secondPayload = secondChunk;
            byte[] firstPhysical = BuildPhysicalRecord(0x0001, firstPayload);
            byte[] secondPhysical = BuildPhysicalRecord(0x0002, secondPayload);
            return Concat(firstPhysical, secondPhysical);
        }

        private static byte[] BuildPhysicalRecord(ushort attributes, byte[] payload)
        {
            ushort length = (ushort)(LisPhysicalRecordHeader.HeaderLength + payload.Length);
            byte[] header = new byte[]
            {
                (byte)(length >> 8), (byte)(length & 0xFF),
                (byte)(attributes >> 8), (byte)(attributes & 0xFF)
            };

            return Concat(header, payload);
        }

        private static byte[] BuildLrhPayload(byte type, byte attributes, byte[] data)
        {
            var payload = new byte[LisLogicalRecordHeader.HeaderLength + data.Length];
            payload[0] = type;
            payload[1] = attributes;
            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, payload, LisLogicalRecordHeader.HeaderLength, data.Length);
            }

            return payload;
        }

        private static byte[] BuildSequence(int length)
        {
            var data = new byte[length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i + 1);
            }

            return data;
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var output = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, output, 0, first.Length);
            Buffer.BlockCopy(second, 0, output, first.Length, second.Length);
            return output;
        }
    }
}
