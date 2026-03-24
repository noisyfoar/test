using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Контейнер в памяти для необработанного представления LIS-потока в виде логических записей.
    /// </summary>
    public sealed class LisDocument
    {
        private readonly List<LisLogicalRecord> _records;

        /// <summary>
        /// Создаёт документ из упорядоченного списка логических записей.
        /// </summary>
        /// <param name="records">
        /// Логические записи в физическом порядке файла; элементы со значением null не допускаются.
        /// </param>
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
