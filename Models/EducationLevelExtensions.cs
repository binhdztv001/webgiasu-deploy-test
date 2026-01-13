namespace Webgiasu.Models
{
    public static class EducationLevelExtensions
    {
        public static string GetDisplayName(this EducationLevel level)
        {
            return level switch
            {
                EducationLevel.TieuHoc => "Tiểu học",
                EducationLevel.THCS => "Trung học cơ sở",
                EducationLevel.THPT => "Trung học phổ thông",
                EducationLevel.DaiHoc => "Đại học",
                _ => "Chưa xác định"
            };
        }

        public static string GetShortName(this EducationLevel level)
        {
            return level switch
            {
                EducationLevel.TieuHoc => "TH",
                EducationLevel.THCS => "THCS",
                EducationLevel.THPT => "THPT",
                EducationLevel.DaiHoc => "ĐH",
                _ => ""
            };
        }
    }
}