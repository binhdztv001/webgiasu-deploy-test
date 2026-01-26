using Webgiasu.Models.ViewModels;

namespace Webgiasu.Services
{
    public interface IStatisticsExportService
    {
        byte[] ExportStudentStatisticsToPdf(StudentStatisticsExportModel data, string studentName);
        byte[] ExportStudentStatisticsToExcel(StudentStatisticsExportModel data, string studentName);
        byte[] ExportTutorStatisticsToPdf(TutorStatisticsExportModel data, string tutorName);
        byte[] ExportTutorStatisticsToExcel(TutorStatisticsExportModel data, string tutorName);
        byte[] ExportSchoolStatisticsToPdf(SchoolStatisticsExportModel data, string schoolName);
        byte[] ExportSchoolStatisticsToExcel(SchoolStatisticsExportModel data, string schoolName);
        byte[] ExportEnterpriseStatisticsToPdf(EnterpriseStatisticsExportModel data, string enterpriseName);
        byte[] ExportEnterpriseStatisticsToExcel(EnterpriseStatisticsExportModel data, string enterpriseName);
    }
}