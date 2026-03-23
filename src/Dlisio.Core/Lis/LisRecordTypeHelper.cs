namespace Dlisio.Core.Lis
{
    public static class LisRecordTypeHelper
    {
        public static bool IsValid(byte value)
        {
            switch ((LisRecordType)value)
            {
                case LisRecordType.NormalData:
                case LisRecordType.AlternateData:
                case LisRecordType.JobIdentification:
                case LisRecordType.WellsiteData:
                case LisRecordType.ToolStringInfo:
                case LisRecordType.EncryptedTableDump:
                case LisRecordType.TableDump:
                case LisRecordType.DataFormatSpecification:
                case LisRecordType.DataDescriptor:
                case LisRecordType.Picture:
                case LisRecordType.Image:
                case LisRecordType.Tu10SoftwareBoot:
                case LisRecordType.BootstrapLoader:
                case LisRecordType.CpKernelLoader:
                case LisRecordType.ProgramFileHeader:
                case LisRecordType.ProgramOverlayHeader:
                case LisRecordType.ProgramOverlayLoad:
                case LisRecordType.FileHeader:
                case LisRecordType.FileTrailer:
                case LisRecordType.TapeHeader:
                case LisRecordType.TapeTrailer:
                case LisRecordType.ReelHeader:
                case LisRecordType.ReelTrailer:
                case LisRecordType.LogicalEof:
                case LisRecordType.LogicalBot:
                case LisRecordType.LogicalEot:
                case LisRecordType.LogicalEom:
                case LisRecordType.OperatorCommandInputs:
                case LisRecordType.OperatorResponseInputs:
                case LisRecordType.SystemOutputs:
                case LisRecordType.FlicComment:
                case LisRecordType.BlankRecord:
                    return true;

                default:
                    return false;
            }
        }
    }
}
