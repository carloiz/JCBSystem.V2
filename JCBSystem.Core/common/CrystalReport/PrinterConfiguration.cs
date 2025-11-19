using System;
using System.Linq;
using System.Drawing.Printing;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using PaperSize = System.Drawing.Printing.PaperSize;
using CrystalPaperSize = CrystalDecisions.Shared.PaperSize;

namespace JCBSystem.Core.common.CrystalReport
{
    public class PrinterConfiguration
    {

        private readonly string printerRecieptName = "Star BSC10"; // Siguraduhin na ito ang tamang printer name

        public void PrintReceipt(ReportDocument repo)
        {
            try
            {
                if (PrinterExists(printerRecieptName))
                {
                    PrinterSettings printerSettings = new PrinterSettings
                    {
                        PrinterName = printerRecieptName
                    };

                    // Custom Paper Size Detection
                    PaperSize selectedPaperSize = printerSettings.PaperSizes.Cast<PaperSize>()
                        .FirstOrDefault(p => p.PaperName.Contains("Receipt") || p.PaperName.Contains("Custom"));

                    if (selectedPaperSize != null)
                    {
                        repo.PrintOptions.PaperSize = (CrystalPaperSize)selectedPaperSize.RawKind;
                    }
                    else
                    {
                        repo.PrintOptions.PaperSize = CrystalPaperSize.DefaultPaperSize;
                    }

                    //// Auto-adjust font size based on paper width
                    //float fontSize = selectedPaperSize != null && selectedPaperSize.Width < 200 ? 6 : 8;
                    //AdjustFontSize(repo, fontSize);

                    // Set Printer and Orientation
                    repo.PrintOptions.PrinterName = printerRecieptName;
                    repo.PrintOptions.PaperOrientation = PaperOrientation.Portrait;

                    //// Set Custom Margins
                    //SetCustomMargins(printerRecieptName);

                    // Print with dynamic height
                    repo.PrintToPrinter(1, false, 1, repo.FormatEngine.GetLastPageNumber(new ReportPageRequestContext()));
                }
                else
                {
                    Console.WriteLine("Thermal printer not found. Please check printer settings.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while printing: {ex.Message}");
            }
        }

        //private void SetCustomMargins(string printerName)
        //{
        //    try
        //    {
        //        // Create PrintDocument object
        //        PrintDocument printDoc = new PrintDocument();
        //        printDoc.PrinterSettings.PrinterName = printerName;

        //        // Set margins to 0 for all sides (no margin)
        //        printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        //        // Apply the margins to the printer settings (if necessary)
        //        printDoc.Print();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error setting custom margins: {ex.Message}");
        //    }
        //}

        //private void AdjustFontSize(ReportDocument repo, float fontSize)
        //{
        //    try
        //    {
        //        foreach (ReportObject obj in repo.ReportDefinition.ReportObjects)
        //        {
        //            if (obj is TextObject textObj)
        //            {
        //                textObj.ApplyFont(new Font(textObj.Font.Name, fontSize));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error adjusting font size: {ex.Message}");
        //    }
        //}







        private bool PrinterExists(string printerName)
        {
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Try to find a default printer if the specific one is not found
                string defaultPrinter = PrinterSettings.InstalledPrinters[0];
                if (defaultPrinter.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log or display the error message
                Console.WriteLine($"Error checking printer: {ex.Message}");
            }

            return false;
        }
    }
}
