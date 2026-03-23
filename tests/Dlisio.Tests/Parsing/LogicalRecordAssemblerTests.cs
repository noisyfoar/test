using System;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class LogicalRecordAssemblerTests
    {
        [Fact]
        public void Assemble_NullSegments_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LogicalRecordAssembler.Assemble(null!));
        }

        [Fact]
        public void Assemble_EmptySegments_ThrowsDlisParseException()
        {
            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(Array.Empty<LogicalRecordSegment>()));

            Assert.Contains("zero segments", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_FirstSegmentNotMarkedAsFirst_ThrowsDlisParseException()
        {
            LogicalRecordSegment only = ParseSegment(0x40, 0x33, 0x20);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { only }));

            Assert.Contains("not marked as first", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_TwoSegmentRecord_ReturnsCombinedBody()
        {
            LogicalRecordSegment first = ParseSegment(0x20, 0x33, 0x10); // successor set
            LogicalRecordSegment second = ParseSegment(0x40, 0x33, 0x20); // predecessor set

            LogicalRecord record = LogicalRecordAssembler.Assemble(new[] { first, second });

            Assert.Equal((byte)0x33, record.LogicalRecordType);
            Assert.Equal(2, record.Segments.Count);
            Assert.Equal(24, record.Body.Length);
            Assert.Equal((byte)0x10, record.Body[0]);
            Assert.Equal((byte)0x1B, record.Body[11]);
            Assert.Equal((byte)0x20, record.Body[12]);
            Assert.Equal((byte)0x2B, record.Body[23]);
        }

        [Fact]
        public void Assemble_RecordTypeChanges_ThrowsDlisParseException()
        {
            LogicalRecordSegment first = ParseSegment(0x20, 0x33, 0x10);
            LogicalRecordSegment second = ParseSegment(0x40, 0x34, 0x20);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { first, second }));

            Assert.Contains("record type", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_MiddleSegmentMarkedAsFirst_ThrowsDlisParseException()
        {
            LogicalRecordSegment first = ParseSegment(0x20, 0x33, 0x10);
            LogicalRecordSegment second = ParseSegment(0x00, 0x33, 0x20);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { first, second }));

            Assert.Contains("non-first", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_FinalSegmentNotMarkedAsLast_ThrowsDlisParseException()
        {
            LogicalRecordSegment first = ParseSegment(0x20, 0x33, 0x10);
            LogicalRecordSegment second = ParseSegment(0x60, 0x33, 0x20);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { first, second }));

            Assert.Contains("final segment", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_StructureBitChanges_ThrowsDlisParseException()
        {
            LogicalRecordSegment first = ParseSegment(0xA0, 0x33, 0x10);  // explicit + successor
            LogicalRecordSegment second = ParseSegment(0x40, 0x33, 0x20); // not explicit

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { first, second }));

            Assert.Contains("structure bit", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_EncryptionBitChanges_ThrowsDlisParseException()
        {
            LogicalRecordSegment first = ParseSegment(0x30, 0x33, 0x10);  // encrypted + successor
            LogicalRecordSegment second = ParseSegment(0x40, 0x33, 0x20); // not encrypted

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { first, second }));

            Assert.Contains("encryption bit", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_EarlyLastSegment_ThrowsDlisParseException()
        {
            LogicalRecordSegment first = ParseSegment(0x00, 0x33, 0x10); // marked as last
            LogicalRecordSegment second = ParseSegment(0x50, 0x33, 0x20);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordAssembler.Assemble(new[] { first, second }));

            Assert.Contains("ended before", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assemble_ZeroLengthBodies_AreHandled()
        {
            LogicalRecordSegment first = ParseSegment(new byte[]
            {
                0x00, 0x10,
                0x38,       // encrypted + packet + successor
                0x33,
                0x00, 0x0C, // encryption packet length = 12, consumes entire payload
                0x12, 0x34,
                0xAA, 0xAB, 0xAC, 0xAD,
                0xAE, 0xAF, 0xB0, 0xB1
            });

            LogicalRecordSegment second = ParseSegment(0x50, 0x33, 0x20);

            LogicalRecord record = LogicalRecordAssembler.Assemble(new[] { first, second });

            Assert.Equal(2, record.Segments.Count);
            Assert.Equal(12, record.Body.Length);
            Assert.Equal((byte)0x20, record.Body[0]);
            Assert.Equal((byte)0x2B, record.Body[11]);
        }

        private static LogicalRecordSegment ParseSegment(byte flags, byte recordType, byte bodyStart)
        {
            return LogicalRecordSegmentParser.Parse(BuildSegment(flags, recordType, bodyStart));
        }

        private static LogicalRecordSegment ParseSegment(byte[] rawSegment)
        {
            return LogicalRecordSegmentParser.Parse(rawSegment);
        }

        private static byte[] BuildSegment(byte flags, byte recordType, byte bodyStart)
        {
            var segment = new byte[16];
            segment[0] = 0x00;
            segment[1] = 0x10;
            segment[2] = flags;
            segment[3] = recordType;

            for (int i = 0; i < 12; i++)
            {
                segment[4 + i] = (byte)(bodyStart + i);
            }

            return segment;
        }
    }
}
