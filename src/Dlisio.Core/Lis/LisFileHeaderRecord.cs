namespace Dlisio.Core.Lis
{
    public sealed class LisFileHeaderRecord
    {
        public LisFileHeaderRecord(
            string fileName,
            string serviceSublevelName,
            string versionNumber,
            string dateOfGeneration,
            string maxPhysicalRecordLength,
            string fileType,
            string previousFileName)
        {
            FileName = fileName;
            ServiceSublevelName = serviceSublevelName;
            VersionNumber = versionNumber;
            DateOfGeneration = dateOfGeneration;
            MaxPhysicalRecordLength = maxPhysicalRecordLength;
            FileType = fileType;
            PreviousFileName = previousFileName;
        }

        public string FileName { get; }

        public string ServiceSublevelName { get; }

        public string VersionNumber { get; }

        public string DateOfGeneration { get; }

        public string MaxPhysicalRecordLength { get; }

        public string FileType { get; }

        public string PreviousFileName { get; }
    }
}
