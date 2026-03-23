using System;
using System.Collections.Generic;

namespace Dlisio.Core.Lis
{
    public sealed class LisDocument
    {
        private readonly List<LisLogicalRecord> _records;

        public LisDocument(IReadOnlyList<LisLogicalRecord> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            _records = new List<LisLogicalRecord>(records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] == null)
                {
                    throw new ArgumentException("LIS document contains a null logical record.", nameof(records));
                }

                _records.Add(records[i]);
            }
        }

        public IReadOnlyList<LisLogicalRecord> Records
        {
            get { return _records; }
        }
    }
}
