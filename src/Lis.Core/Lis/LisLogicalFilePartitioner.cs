using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisLogicalFilePartitioner
    {
        /// <summary>
        /// Подробно выполняет операцию «Partition» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public IReadOnlyList<LisLogicalFile> Partition(LisRecordIndex index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            var files = new List<LisLogicalFile>();
            var currentRecords = new List<LisRecordInfo>();
            LisRecordInfo? currentHeader = null;

            for (int i = 0; i < index.Records.Count; i++)
            {
                LisRecordInfo current = index.Records[i];

                if (current.Type == LisRecordType.FileHeader)
                {
                    if (currentRecords.Count > 0)
                    {
                        files.Add(new LisLogicalFile(
                            currentRecords,
                            currentHeader,
                            fileTrailer: null,
                            isComplete: false));

                        currentRecords = new List<LisRecordInfo>();
                    }

                    currentHeader = current;
                    currentRecords.Add(current);
                    continue;
                }

                if (currentRecords.Count == 0)
                {
                    // Игнорируем записи до первого заголовка файла.
                    continue;
                }

                currentRecords.Add(current);

                if (current.Type == LisRecordType.FileTrailer)
                {
                    files.Add(new LisLogicalFile(
                        currentRecords,
                        currentHeader,
                        fileTrailer: current,
                        isComplete: true));

                    currentRecords = new List<LisRecordInfo>();
                    currentHeader = null;
                }
            }

            if (currentRecords.Count > 0)
            {
                files.Add(new LisLogicalFile(
                    currentRecords,
                    currentHeader,
                    fileTrailer: null,
                    isComplete: false));
            }

            return files;
        }
    }
}
