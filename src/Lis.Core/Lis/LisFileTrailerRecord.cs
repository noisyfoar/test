namespace Lis.Core.Lis
{
    public sealed class LisFileTrailerRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisFileTrailerRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisFileTrailerRecord(
            string fileName,
            string serviceSublevelName,
            string versionNumber,
            string dateOfGeneration,
            string maxPhysicalRecordLength,
            string fileType,
            string nextFileName)
        {
            FileName = fileName;
            ServiceSublevelName = serviceSublevelName;
            VersionNumber = versionNumber;
            DateOfGeneration = dateOfGeneration;
            MaxPhysicalRecordLength = maxPhysicalRecordLength;
            FileType = fileType;
            NextFileName = nextFileName;
        }

        public string FileName { get; }

        public string ServiceSublevelName { get; }

        public string VersionNumber { get; }

        public string DateOfGeneration { get; }

        public string MaxPhysicalRecordLength { get; }

        public string FileType { get; }

        public string NextFileName { get; }
    }
}
