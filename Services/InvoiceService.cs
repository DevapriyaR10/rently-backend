using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;
using Rently.Api.Models;

namespace Rently.Api.Services
{
    public class InvoiceService
    {
        public async Task<byte[]> GenerateInvoiceAsync(Payment payment)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(40);
                        page.Header().Text("Rently Invoice").FontSize(24).Bold().AlignCenter();

                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Item().Text($"Invoice ID: {payment.PaymentId}");
                            col.Item().Text($"Payment Date: {payment.PaymentDate:dd-MMM-yyyy}");
                            col.Item().Text($"Payment Method: {payment.PaymentMethod}");
                            col.Item().Text($"Amount: ₹{payment.Amount:F2}");
                            col.Item().Text($"Status: {payment.Status}");
                            col.Item().Text($"Notes: {payment.Notes ?? "N/A"}");
                        });

                        page.Footer().AlignCenter().Text("Thank you for choosing Rently!");
                    });
                });

                using var stream = new MemoryStream();
                document.GeneratePdf(stream);
                return stream.ToArray();
            });
        }
    }
}
