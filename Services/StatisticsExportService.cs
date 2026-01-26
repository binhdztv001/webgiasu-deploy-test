using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Webgiasu.Models.ViewModels;

namespace Webgiasu.Services
{
    public class StatisticsExportService : IStatisticsExportService
    {
        // ✅ HELPER METHOD - Chuyển đổi tên môn học
        private string GetSubjectDisplayName(string subjectType)
        {
            return subjectType switch
            {
                "Toan" => "Toán",
                "Ly" => "Vật Lý",
                "Hoa" => "Hóa Học",
                "Sinh" => "Sinh Học",
                "Van" => "Ngữ Văn",
                "Anh" => "Tiếng Anh",
                "Su" => "Lịch Sử",
                "Dia" => "Địa Lý",
                "Khac" => "Khác",
                _ => subjectType
            };
        }

        // ===== STUDENT EXPORTS =====

        public byte[] ExportStudentStatisticsToPdf(StudentStatisticsExportModel data, string studentName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Text($"THỐNG KÊ HỌC TẬP - {studentName}")
                        .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // Ngày xuất
                            column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);

                            // Tổng quan
                            column.Item().Text("TỔNG QUAN").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(200);
                                    columns.RelativeColumn();
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng bài toán").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Đã hoàn thành").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.SolvedProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Đang giải").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.InProgressProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Chờ Mentor").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.WaitingProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng chi tiêu").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text($"{data.TotalSpent:N0} đ");
                            });

                            // Theo môn học
                            column.Item().PaddingTop(10).Text("THEO MÔN HỌC").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Môn học").SemiBold();
                                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Số bài").SemiBold();
                                });

                                foreach (var item in data.ProblemsByType)
                                {
                                    // ✅ CHUYỂN ĐỔI SANG TIẾNG VIỆT
                                    var displayName = GetSubjectDisplayName(item.Type);
                                    
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(displayName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                }
                            });

                            // Theo cấp học
                            column.Item().PaddingTop(10).Text("THEO CẤP HỌC").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Cấp học").SemiBold();
                                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Số bài").SemiBold();
                                });

                                foreach (var item in data.ProblemsByDifficulty)
                                {
                                    var levelText = item.Difficulty == "TieuHoc" ? "Tiểu học" :
                                                   item.Difficulty == "THCS" ? "THCS" :
                                                   item.Difficulty == "THPT" ? "THPT" :
                                                   item.Difficulty == "DaiHoc" ? "Đại học" : item.Difficulty;
                                    
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(levelText);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportStudentStatisticsToExcel(StudentStatisticsExportModel data, string studentName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Thống kê");

            // Header
            worksheet.Cell(1, 1).Value = $"THỐNG KÊ HỌC TẬP - {studentName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.Blue;

            worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Tổng quan
            int row = 4;
            worksheet.Cell(row, 1).Value = "TỔNG QUAN";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng bài toán";
            worksheet.Cell(row, 2).Value = data.TotalProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Đã hoàn thành";
            worksheet.Cell(row, 2).Value = data.SolvedProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Đang giải";
            worksheet.Cell(row, 2).Value = data.InProgressProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Chờ Mentor";
            worksheet.Cell(row, 2).Value = data.WaitingProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng chi tiêu";
            worksheet.Cell(row, 2).Value = $"{data.TotalSpent:N0} đ";
            row += 2;

            // Theo môn học 
            worksheet.Cell(row, 1).Value = "THEO MÔN HỌC";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Môn học";
            worksheet.Cell(row, 2).Value = "Số bài";
            worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
            row++;

            foreach (var item in data.ProblemsByType)
            {
                // ✅ CHUYỂN ĐỔI SANG TIẾNG VIỆT
                var displayName = GetSubjectDisplayName(item.Type);
                
                worksheet.Cell(row, 1).Value = displayName;
                worksheet.Cell(row, 2).Value = item.Count;
                row++;
            }

            row++;

            // Theo cấp học
            worksheet.Cell(row, 1).Value = "THEO CẤP HỌC";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Cấp học";
            worksheet.Cell(row, 2).Value = "Số bài";
            worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
            row++;

            foreach (var item in data.ProblemsByDifficulty)
            {
                var levelText = item.Difficulty == "TieuHoc" ? "Tiểu học" :
                               item.Difficulty == "THCS" ? "THCS" :
                               item.Difficulty == "THPT" ? "THPT" :
                               item.Difficulty == "DaiHoc" ? "Đại học" : item.Difficulty;

                worksheet.Cell(row, 1).Value = levelText;
                worksheet.Cell(row, 2).Value = item.Count;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ===== TUTOR EXPORTS =====

        public byte[] ExportTutorStatisticsToPdf(TutorStatisticsExportModel data, string tutorName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Text($"THỐNG KÊ GIẢNG DẠY - {tutorName}")
                        .SemiBold().FontSize(18).FontColor(Colors.Green.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);

                            // Tổng quan
                            column.Item().Text("TỔNG QUAN").SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(200);
                                    columns.RelativeColumn();
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng bài đã nhận").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Đã hoàn thành").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.SolvedProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Đang giải").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.InProgressProblems.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng thu nhập").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text($"{data.TotalEarnings:N0} đ");
                            });

                            // Theo môn học - ✅ SỬA ĐỔI Ở ĐÂY
                            column.Item().PaddingTop(10).Text("THEO MÔN HỌC").SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Môn học").SemiBold();
                                    header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Số bài").SemiBold();
                                });

                                foreach (var item in data.ProblemsByType)
                                {
                                    // ✅ CHUYỂN ĐỔI SANG TIẾNG VIỆT
                                    var displayName = GetSubjectDisplayName(item.Type);
                                    
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(displayName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                }
                            });

                            // Theo cấp học
                            column.Item().PaddingTop(10).Text("THEO CẤP HỌC").SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Cấp học").SemiBold();
                                    header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Số bài").SemiBold();
                                });

                                foreach (var item in data.ProblemsByDifficulty)
                                {
                                    var levelText = item.Difficulty == "TieuHoc" ? "Tiểu học" :
                                                   item.Difficulty == "THCS" ? "THCS" :
                                                   item.Difficulty == "THPT" ? "THPT" :
                                                   item.Difficulty == "DaiHoc" ? "Đại học" : item.Difficulty;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(levelText);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportTutorStatisticsToExcel(TutorStatisticsExportModel data, string tutorName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Thống kê");

            // Header
            worksheet.Cell(1, 1).Value = $"THỐNG KÊ GIẢNG DẠY - {tutorName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.Green;

            worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Tổng quan
            int row = 4;
            worksheet.Cell(row, 1).Value = "TỔNG QUAN";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng bài đã nhận";
            worksheet.Cell(row, 2).Value = data.TotalProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Đã hoàn thành";
            worksheet.Cell(row, 2).Value = data.SolvedProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Đang giải";
            worksheet.Cell(row, 2).Value = data.InProgressProblems;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng thu nhập";
            worksheet.Cell(row, 2).Value = $"{data.TotalEarnings:N0} đ";
            row += 2;

            // Theo môn học - ✅ SỬA ĐỔI Ở ĐÂY
            worksheet.Cell(row, 1).Value = "THEO MÔN HỌC";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Môn học";
            worksheet.Cell(row, 2).Value = "Số bài";
            worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
            row++;

            foreach (var item in data.ProblemsByType)
            {
                // ✅ CHUYỂN ĐỔI SANG TIẾNG VIỆT
                var displayName = GetSubjectDisplayName(item.Type);
                
                worksheet.Cell(row, 1).Value = displayName;
                worksheet.Cell(row, 2).Value = item.Count;
                row++;
            }

            row++;

            // Theo cấp học
            worksheet.Cell(row, 1).Value = "THEO CẤP HỌC";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Cấp học";
            worksheet.Cell(row, 2).Value = "Số bài";
            worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
            row++;

            foreach (var item in data.ProblemsByDifficulty)
            {
                var levelText = item.Difficulty == "TieuHoc" ? "Tiểu học" :
                               item.Difficulty == "THCS" ? "THCS" :
                               item.Difficulty == "THPT" ? "THPT" :
                               item.Difficulty == "DaiHoc" ? "Đại học" : item.Difficulty;

                worksheet.Cell(row, 1).Value = levelText;
                worksheet.Cell(row, 2).Value = item.Count;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ===== SCHOOL EXPORTS =====

        public byte[] ExportSchoolStatisticsToPdf(SchoolStatisticsExportModel data, string schoolName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Text($"THỐNG KÊ NHÀ TRƯỜNG - {schoolName}")
                        .SemiBold().FontSize(18).FontColor(Colors.Orange.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);

                            // Tổng quan
                            column.Item().Text("TỔNG QUAN").SemiBold().FontSize(14).FontColor(Colors.Orange.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(200);
                                    columns.RelativeColumn();
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng số lớp").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalClasses.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Lớp đang hoạt động").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.ActiveClasses.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Lớp đã hoàn thành").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.CompletedClasses.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng học sinh").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalStudents.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng Mentor").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalTutors.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tỷ lệ hoàn thành").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text($"{data.CompletionRate:F1}%");
                            });

                            // Theo môn học
                            if (data.ClassesBySubject.Any())
                            {
                                column.Item().PaddingTop(10).Text("THEO MÔN HỌC").SemiBold().FontSize(14).FontColor(Colors.Orange.Darken2);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Môn học").SemiBold();
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Số lớp").SemiBold();
                                    });

                                    foreach (var item in data.ClassesBySubject)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Subject);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                    }
                                });
                            }

                            // Theo trạng thái
                            if (data.ClassesByStatus.Any())
                            {
                                column.Item().PaddingTop(10).Text("THEO TRẠNG THÁI").SemiBold().FontSize(14).FontColor(Colors.Orange.Darken2);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Trạng thái").SemiBold();
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Số lớp").SemiBold();
                                    });

                                    foreach (var item in data.ClassesByStatus)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Status);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                    }
                                });
                            }

                            // Danh sách lớp học (Top 10)
                            if (data.ClassDetails.Any())
                            {
                                column.Item().PaddingTop(10).Text("DANH SÁCH LỚP HỌC (TOP 10)").SemiBold().FontSize(14).FontColor(Colors.Orange.Darken2);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(50);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Lớp học").SemiBold();
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Môn").SemiBold();
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Mentor").SemiBold();
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("HS").SemiBold();
                                        header.Cell().Background(Colors.Orange.Lighten3).Padding(5).Text("Trạng thái").SemiBold();
                                    });

                                    foreach (var item in data.ClassDetails.Take(10))
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ClassName);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Subject);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.TutorName);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.StudentCount.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Status);
                                    }
                                });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportSchoolStatisticsToExcel(SchoolStatisticsExportModel data, string schoolName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Thống kê");

            // Header
            worksheet.Cell(1, 1).Value = $"THỐNG KÊ NHÀ TRƯỜNG - {schoolName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.Orange;

            worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Tổng quan
            int row = 4;
            worksheet.Cell(row, 1).Value = "TỔNG QUAN";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng số lớp";
            worksheet.Cell(row, 2).Value = data.TotalClasses;
            row++;

            worksheet.Cell(row, 1).Value = "Lớp đang hoạt động";
            worksheet.Cell(row, 2).Value = data.ActiveClasses;
            row++;

            worksheet.Cell(row, 1).Value = "Lớp đã hoàn thành";
            worksheet.Cell(row, 2).Value = data.CompletedClasses;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng học sinh";
            worksheet.Cell(row, 2).Value = data.TotalStudents;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng Mentor";
            worksheet.Cell(row, 2).Value = data.TotalTutors;
            row++;

            worksheet.Cell(row, 1).Value = "Tỷ lệ hoàn thành";
            worksheet.Cell(row, 2).Value = $"{data.CompletionRate:F1}%";
            row += 2;

            // Theo môn học
            if (data.ClassesBySubject.Any())
            {
                worksheet.Cell(row, 1).Value = "THEO MÔN HỌC";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                row++;

                worksheet.Cell(row, 1).Value = "Môn học";
                worksheet.Cell(row, 2).Value = "Số lớp";
                worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 224, 178);
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
                row++;

                foreach (var item in data.ClassesBySubject)
                {
                    worksheet.Cell(row, 1).Value = item.Subject;
                    worksheet.Cell(row, 2).Value = item.Count;
                    row++;
                }
                row++;
            }

            // Theo trạng thái
            if (data.ClassesByStatus.Any())
            {
                worksheet.Cell(row, 1).Value = "THEO TRẠNG THÁI";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                row++;

                worksheet.Cell(row, 1).Value = "Trạng thái";
                worksheet.Cell(row, 2).Value = "Số lớp";
                worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 224, 178);
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
                row++;

                foreach (var item in data.ClassesByStatus)
                {
                    worksheet.Cell(row, 1).Value = item.Status;
                    worksheet.Cell(row, 2).Value = item.Count;
                    row++;
                }
                row++;
            }

            // Danh sách lớp học
            if (data.ClassDetails.Any())
            {
                worksheet.Cell(row, 1).Value = "DANH SÁCH LỚP HỌC";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                row++;

                worksheet.Cell(row, 1).Value = "Lớp học";
                worksheet.Cell(row, 2).Value = "Môn học";
                worksheet.Cell(row, 3).Value = "Mentor";
                worksheet.Cell(row, 4).Value = "Số HS";
                worksheet.Cell(row, 5).Value = "Trạng thái";
                worksheet.Cell(row, 6).Value = "Ngày bắt đầu";
                worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 224, 178);
                worksheet.Range(row, 1, row, 6).Style.Font.Bold = true;
                row++;

                foreach (var item in data.ClassDetails)
                {
                    worksheet.Cell(row, 1).Value = item.ClassName;
                    worksheet.Cell(row, 2).Value = item.Subject;
                    worksheet.Cell(row, 3).Value = item.TutorName;
                    worksheet.Cell(row, 4).Value = item.StudentCount;
                    worksheet.Cell(row, 5).Value = item.Status;
                    worksheet.Cell(row, 6).Value = item.StartDate;
                    row++;
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ===== ENTERPRISE EXPORTS =====

        public byte[] ExportEnterpriseStatisticsToPdf(EnterpriseStatisticsExportModel data, string enterpriseName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Text($"THỐNG KÊ HỆ THỐNG - {enterpriseName}")
                        .SemiBold().FontSize(18).FontColor(Colors.Brown.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);

                            // Tổng quan
                            column.Item().Text("TỔNG QUAN HỆ THỐNG").SemiBold().FontSize(14).FontColor(Colors.Brown.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(200);
                                    columns.RelativeColumn();
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng số trường").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalSchools.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng Mentor").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalMentors.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng lớp học").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalClasses.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Lớp đang hoạt động").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.ActiveClasses.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng học sinh").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalStudents.ToString());

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Tổng lịch trao đổi").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(data.TotalSchedules.ToString());
                            });

                            // Thống kê theo trường
                            if (data.SchoolStats.Any())
                            {
                                column.Item().PaddingTop(10).Text("THỐNG KÊ THEO TRƯỜNG").SemiBold().FontSize(14).FontColor(Colors.Brown.Darken2);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Tên trường").SemiBold();
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Tổng lớp").SemiBold();
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Hoạt động").SemiBold();
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Học sinh").SemiBold();
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Lịch").SemiBold();
                                    });

                                    foreach (var item in data.SchoolStats)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.SchoolName);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.TotalClasses.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ActiveClasses.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.TotalStudents.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.TotalSchedules.ToString());
                                    }
                                });
                            }

                            // Mentor theo môn học
                            if (data.MentorsBySubject.Any())
                            {
                                column.Item().PaddingTop(10).Text("MENTOR THEO MÔN HỌC").SemiBold().FontSize(14).FontColor(Colors.Brown.Darken2);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Môn học").SemiBold();
                                        header.Cell().Background(Colors.Brown.Lighten3).Padding(5).Text("Số Mentor").SemiBold();
                                    });

                                    foreach (var item in data.MentorsBySubject)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Subject);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                                    }
                                });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportEnterpriseStatisticsToExcel(EnterpriseStatisticsExportModel data, string enterpriseName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Thống kê");

            // Header
            worksheet.Cell(1, 1).Value = $"THỐNG KÊ HỆ THỐNG - {enterpriseName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(121, 85, 72); // Brown color

            worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Tổng quan
            int row = 4;
            worksheet.Cell(row, 1).Value = "TỔNG QUAN HỆ THỐNG";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng số trường";
            worksheet.Cell(row, 2).Value = data.TotalSchools;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng Mentor";
            worksheet.Cell(row, 2).Value = data.TotalMentors;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng lớp học";
            worksheet.Cell(row, 2).Value = data.TotalClasses;
            row++;

            worksheet.Cell(row, 1).Value = "Lớp đang hoạt động";
            worksheet.Cell(row, 2).Value = data.ActiveClasses;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng học sinh";
            worksheet.Cell(row, 2).Value = data.TotalStudents;
            row++;

            worksheet.Cell(row, 1).Value = "Tổng lịch trao đổi";
            worksheet.Cell(row, 2).Value = data.TotalSchedules;
            row += 2;

            // Thống kê theo trường
            if (data.SchoolStats.Any())
            {
                worksheet.Cell(row, 1).Value = "THỐNG KÊ THEO TRƯỜNG";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                row++;

                worksheet.Cell(row, 1).Value = "Tên trường";
                worksheet.Cell(row, 2).Value = "Tổng lớp";
                worksheet.Cell(row, 3).Value = "Hoạt động";
                worksheet.Cell(row, 4).Value = "Học sinh";
                worksheet.Cell(row, 5).Value = "Lịch trao đổi";
                worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromArgb(215, 204, 200); // Light brown
                worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                row++;

                foreach (var item in data.SchoolStats)
                {
                    worksheet.Cell(row, 1).Value = item.SchoolName;
                    worksheet.Cell(row, 2).Value = item.TotalClasses;
                    worksheet.Cell(row, 3).Value = item.ActiveClasses;
                    worksheet.Cell(row, 4).Value = item.TotalStudents;
                    worksheet.Cell(row, 5).Value = item.TotalSchedules;
                    row++;
                }
                row++;
            }

            // Mentor theo môn học
            if (data.MentorsBySubject.Any())
            {
                worksheet.Cell(row, 1).Value = "MENTOR THEO MÔN HỌC";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                row++;

                worksheet.Cell(row, 1).Value = "Môn học";
                worksheet.Cell(row, 2).Value = "Số Mentor";
                worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(215, 204, 200);
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
                row++;

                foreach (var item in data.MentorsBySubject)
                {
                    worksheet.Cell(row, 1).Value = item.Subject;
                    worksheet.Cell(row, 2).Value = item.Count;
                    row++;
                }
                row++;
            }

            // Lịch trao đổi theo trạng thái
            if (data.SchedulesByStatus.Any())
            {
                worksheet.Cell(row, 1).Value = "LỊCH TRAO ĐỔI THEO TRẠNG THÁI";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                row++;

                worksheet.Cell(row, 1).Value = "Trạng thái";
                worksheet.Cell(row, 2).Value = "Số lịch";
                worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(215, 204, 200);
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
                row++;

                foreach (var item in data.SchedulesByStatus)
                {
                    worksheet.Cell(row, 1).Value = item.Status;
                    worksheet.Cell(row, 2).Value = item.Count;
                    row++;
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}