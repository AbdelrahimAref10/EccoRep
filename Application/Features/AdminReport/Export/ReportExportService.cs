using Application.Features.AdminReport.Common;
using Application.Features.AdminReport.DTOs;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Features.AdminReport.Export
{
    public interface IReportExportService
    {
        ReportFileResult ExportOrdersDetails(OrdersDetailsReportDto report, ReportExportFormat format);
        ReportFileResult ExportCancelledOrders(CancelledOrdersReportDto report, ReportExportFormat format);
        ReportFileResult ExportCancellationDebts(CancellationDebtsReportDto report, ReportExportFormat format);
        ReportFileResult ExportPayments(PaymentsReportDto report, ReportExportFormat format);
        ReportFileResult ExportPayPalRefunds(PayPalRefundsReportDto report, ReportExportFormat format);
    }

    public class ReportExportService : IReportExportService
    {
        static ReportExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ReportFileResult ExportOrdersDetails(OrdersDetailsReportDto report, ReportExportFormat format)
        {
            var headers = new[]
            {
                "Order Code", "Customer", "Mobile", "City", "SubCategory", "From", "To",
                "Vehicles", "SubTotal", "Previous Debt", "Total", "Payment", "Payment State",
                "Order State", "Cancelled", "Money Refunded", "Created"
            };

            var rows = report.Items.Select(i => new object[]
            {
                i.OrderCode,
                i.CustomerName,
                i.CustomerMobile,
                i.CityName,
                i.SubCategoryName,
                i.ReservationDateFrom.ToString("yyyy-MM-dd"),
                i.ReservationDateTo.ToString("yyyy-MM-dd"),
                i.VehiclesCount,
                i.OrderSubTotal,
                i.PreviousDebt,
                i.OrderTotal,
                i.PaymentMethod.ToString(),
                i.PaymentState?.ToString() ?? "-",
                i.OrderState.ToString(),
                i.IsCancelled ? "Yes" : "No",
                i.MoneyRefunded ? "Yes" : "No",
                i.CreatedDate.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            var totals = new[]
            {
                $"Orders: {report.Totals.OrdersCount}",
                $"SubTotal: {report.Totals.TotalSubTotal:0.00}",
                $"Previous Debt: {report.Totals.TotalPreviousDebt:0.00}",
                $"Order Total: {report.Totals.TotalOrderAmount:0.00}",
                $"Paid: {report.Totals.TotalPaidAmount:0.00}"
            };

            return Export("OrdersDetails", headers, rows, totals, format);
        }

        public ReportFileResult ExportCancelledOrders(CancelledOrdersReportDto report, ReportExportFormat format)
        {
            var headers = new[]
            {
                "Order Code", "Customer", "Mobile", "City", "Order Total", "Previous Debt",
                "Payment", "Cancellation Fees", "Fee State", "Fee Paid", "Refundable PayPal",
                "PayPal Refund State", "Money Refunded", "Created", "Cancelled"
            };

            var rows = report.Items.Select(i => new object[]
            {
                i.OrderCode,
                i.CustomerName,
                i.CustomerMobile,
                i.CityName,
                i.OrderTotal,
                i.PreviousDebt,
                i.PaymentMethod.ToString(),
                i.CancellationFees,
                i.CancellationFeeState?.ToString() ?? "-",
                i.CancellationFeePaid ? "Yes" : "No",
                i.RefundablePaypalAmount,
                i.PaypalRefundState?.ToString() ?? "-",
                i.MoneyRefunded ? "Yes" : "No",
                i.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                i.CancelledDate?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            }).ToList();

            var totals = new[]
            {
                $"Orders: {report.Totals.OrdersCount}",
                $"Order Total: {report.Totals.TotalOrderAmount:0.00}",
                $"Cancellation Fees: {report.Totals.TotalCancellationFees:0.00}",
                $"Paid Fees: {report.Totals.TotalPaidCancellationFees:0.00}",
                $"Unpaid Fees: {report.Totals.TotalUnpaidCancellationFees:0.00}",
                $"Refundable PayPal: {report.Totals.TotalRefundablePaypal:0.00}"
            };

            return Export("CancelledOrders", headers, rows, totals, format);
        }

        public ReportFileResult ExportCancellationDebts(CancellationDebtsReportDto report, ReportExportFormat format)
        {
            var headers = new[]
            {
                "Customer", "Mobile", "Order Code", "Amount", "State", "Description", "Created"
            };

            var rows = report.Items.Select(i => new object[]
            {
                i.CustomerName,
                i.CustomerMobile,
                i.OrderCode ?? "-",
                i.Amount,
                i.State.ToString(),
                i.Description,
                i.CreatedDate.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            var totals = new[]
            {
                $"Entries: {report.Totals.EntriesCount}",
                $"Customers: {report.Totals.CustomersCount}",
                $"Total: {report.Totals.TotalAmount:0.00}",
                $"Pending: {report.Totals.TotalPending:0.00}",
                $"UnderPayment: {report.Totals.TotalUnderPayment:0.00}",
                $"Paid: {report.Totals.TotalPaid:0.00}"
            };

            return Export("CancellationDebts", headers, rows, totals, format);
        }

        public ReportFileResult ExportPayments(PaymentsReportDto report, ReportExportFormat format)
        {
            var headers = new[]
            {
                "Order Code", "Customer", "City", "Method", "State", "Amount", "Previous Debt", "Order State", "Created"
            };

            var rows = report.Items.Select(i => new object[]
            {
                i.OrderCode,
                i.CustomerName,
                i.CityName,
                i.PaymentMethod.ToString(),
                i.State.ToString(),
                i.Amount,
                i.PreviousDebt,
                i.OrderState.ToString(),
                i.CreatedDate.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            var totals = new[]
            {
                $"Payments: {report.Totals.PaymentsCount}",
                $"Total: {report.Totals.TotalAmount:0.00}",
                $"Paid: {report.Totals.TotalPaid:0.00}",
                $"Pending: {report.Totals.TotalPending:0.00}",
                $"Failed: {report.Totals.TotalFailed:0.00}",
                $"Refunded: {report.Totals.TotalRefunded:0.00}",
                $"Cash Paid: {report.Totals.TotalCashPaid:0.00}",
                $"PayPal Paid: {report.Totals.TotalPayPalPaid:0.00}"
            };

            return Export("Payments", headers, rows, totals, format);
        }

        public ReportFileResult ExportPayPalRefunds(PayPalRefundsReportDto report, ReportExportFormat format)
        {
            var headers = new[]
            {
                "Order Code", "Customer", "Mobile", "Order Total", "Cancellation Fees",
                "Refundable", "State", "Money Refunded", "Created"
            };

            var rows = report.Items.Select(i => new object[]
            {
                i.OrderCode,
                i.CustomerName,
                i.CustomerMobile,
                i.OrderTotal,
                i.CancellationFees,
                i.RefundableAmount,
                i.State.ToString(),
                i.MoneyRefunded ? "Yes" : "No",
                i.CreatedDate.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            var totals = new[]
            {
                $"Entries: {report.Totals.EntriesCount}",
                $"Order Total: {report.Totals.TotalOrderAmount:0.00}",
                $"Cancellation Fees: {report.Totals.TotalCancellationFees:0.00}",
                $"Refundable: {report.Totals.TotalRefundable:0.00}",
                $"Pending: {report.Totals.TotalPendingRefundable:0.00}",
                $"Completed: {report.Totals.TotalCompletedRefundable:0.00}"
            };

            return Export("PayPalRefunds", headers, rows, totals, format);
        }

        private static ReportFileResult Export(
            string reportName,
            IReadOnlyList<string> headers,
            IReadOnlyList<object[]> rows,
            IReadOnlyList<string> totals,
            ReportExportFormat format)
        {
            return format switch
            {
                ReportExportFormat.Excel => ToExcel(reportName, headers, rows, totals),
                ReportExportFormat.Pdf => ToPdf(reportName, headers, rows, totals),
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        private static ReportFileResult ToExcel(
            string reportName,
            IReadOnlyList<string> headers,
            IReadOnlyList<object[]> rows,
            IReadOnlyList<string> totals)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(reportName.Length > 31 ? reportName[..31] : reportName);

            for (var c = 0; c < headers.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
            }

            var headerRange = sheet.Range(1, 1, 1, headers.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
            headerRange.Style.Font.FontColor = XLColor.White;

            for (var r = 0; r < rows.Count; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                {
                    var value = rows[r][c];
                    var cell = sheet.Cell(r + 2, c + 1);
                    switch (value)
                    {
                        case decimal d:
                            cell.Value = d;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                            break;
                        case int i:
                            cell.Value = i;
                            break;
                        case null:
                            cell.Value = "-";
                            break;
                        default:
                            cell.Value = value.ToString();
                            break;
                    }
                }
            }

            var totalsRow = rows.Count + 3;
            sheet.Cell(totalsRow, 1).Value = "Totals";
            sheet.Cell(totalsRow, 1).Style.Font.Bold = true;
            for (var i = 0; i < totals.Count; i++)
            {
                sheet.Cell(totalsRow + 1 + i, 1).Value = totals[i];
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new ReportFileResult
            {
                Content = stream.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"{reportName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx"
            };
        }

        private static ReportFileResult ToPdf(
            string reportName,
            IReadOnlyList<string> headers,
            IReadOnlyList<object[]> rows,
            IReadOnlyList<string> totals)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"ECCO — {reportName}").SemiBold().FontSize(14);
                        col.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(8);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var _ in headers)
                                {
                                    columns.RelativeColumn();
                                }
                            });

                            table.Header(header =>
                            {
                                foreach (var h in headers)
                                {
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(3)
                                        .Text(h).FontColor(Colors.White).SemiBold();
                                }
                            });

                            foreach (var row in rows)
                            {
                                foreach (var cell in row)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(FormatCell(cell));
                                }
                            }
                        });

                        col.Item().PaddingTop(12).Text("Totals").SemiBold().FontSize(10);
                        foreach (var total in totals)
                        {
                            col.Item().Text(total);
                        }
                    });
                });
            });

            return new ReportFileResult
            {
                Content = document.GeneratePdf(),
                ContentType = "application/pdf",
                FileName = $"{reportName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf"
            };
        }

        private static string FormatCell(object? value) => value switch
        {
            null => "-",
            decimal d => d.ToString("0.00"),
            _ => value.ToString() ?? "-"
        };
    }
}
