using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MiSistemaAsistencia.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace MiSistemaAsistencia.Infrastructure.Reporting
{
    public class AttendanceReport : IDocument
    {
        private readonly List<AttendanceReportEntry> _records;
        //private readonly List<AttendanceRecord> _records;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;

        public AttendanceReport(List<AttendanceReportEntry> records, DateTime startDate, DateTime endDate)
        {
            _records = records;
            _startDate = startDate;
            _endDate = endDate;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"Reporte de Asistencia")
                    .Bold().FontSize(20);

                col.Item().Text($"Desde: {_startDate:dd/MM/yyyy} - Hasta: {_endDate:dd/MM/yyyy}");
                col.Item().PaddingTop(10);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3); // Empleado
                    columns.RelativeColumn(2); // Fecha
                    columns.RelativeColumn(2); // Entrada
                    columns.RelativeColumn(2); // Salida
                    columns.RelativeColumn(1); // Horas
                });

                // Cabecera de la tabla
                table.Header(header =>
                {
                    header.Cell().Text("EmpleadoID").Bold();
                    header.Cell().Text("Empleado").Bold();
                    header.Cell().Text("Fecha").Bold();
                    header.Cell().Text("Entrada").Bold();
                    header.Cell().Text("Salida").Bold();
                    header.Cell().Text("Total").Bold();
                });

                // Filas de datos
                foreach (var record in _records)
                {
                    var totalHours = record.CheckOutTime.HasValue ?
                        (record.CheckOutTime.Value - record.CheckInTime).TotalHours : 0;

                    table.Cell().Text(record.EmployeeNumber);
                    table.Cell().Text(record.FullName);
                    table.Cell().Text(record.CheckInTime.ToString("dd/MM/yyyy"));
                    table.Cell().Text(record.CheckInTime.ToString("hh:mm tt"));
                    table.Cell().Text(record.CheckOutTime.HasValue ? record.CheckOutTime.Value.ToString("hh:mm tt") : "N/A");
                    table.Cell().Text(totalHours.ToString("F2"));
                }
            });
        }
    }
}