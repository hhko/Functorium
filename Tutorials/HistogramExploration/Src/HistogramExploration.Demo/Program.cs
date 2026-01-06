using HistogramExploration.Demo.Basic;
using HistogramExploration.Demo.Intermediate;
using HistogramExploration.Demo.Advanced;

namespace HistogramExploration.Demo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    Histogram Exploration Tutorial                          ║");
        Console.WriteLine("║                  Microsoft Learn & Functorium Examples                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (args.Length > 0 && int.TryParse(args[0], out int exampleNumber))
        {
            RunExample(exampleNumber);
            return;
        }

        ShowMenu();
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("Select an example to run:");
            Console.WriteLine();
            Console.WriteLine("BASIC LEVEL:");
            Console.WriteLine("  1. Basic01 - Simple Histogram");
            Console.WriteLine("  2. Basic02 - Histogram with Tags");
            Console.WriteLine("  3. Basic03 - Histogram Units");
            Console.WriteLine("  4. Basic04 - Understanding Percentiles ⭐");
            Console.WriteLine();
            Console.WriteLine("INTERMEDIATE LEVEL:");
            Console.WriteLine("  5. Intermediate01 - Custom Buckets");
            Console.WriteLine("  6. Intermediate02 - Multiple Histograms");
            Console.WriteLine("  7. Intermediate03 - Tag Combinations");
            Console.WriteLine();
            Console.WriteLine("ADVANCED LEVEL:");
            Console.WriteLine("  8. Advanced01 - InstrumentAdvice API");
            Console.WriteLine("  9. Advanced02 - SLO-Aligned Buckets");
            Console.WriteLine(" 10. Advanced03 - Request Latency Scenario");
            Console.WriteLine(" 11. Advanced04 - Database Query Scenario");
            Console.WriteLine(" 12. Advanced05 - Order Processing Scenario");
            Console.WriteLine(" 13. Advanced06 - Bucket Alignment Impact ⭐ (핵심 개념)");
            Console.WriteLine();
            Console.WriteLine("  0. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice: ");

            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input == "0")
                break;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 13)
            {
                Console.WriteLine();
                RunExample(choice);
                Console.WriteLine();
                Console.WriteLine("Press any key to return to menu...");
                Console.ReadKey();
                Console.Clear();
            }
            else
            {
                Console.WriteLine("Invalid choice. Please try again.");
                Console.WriteLine();
            }
        }
    }

    static void RunExample(int number)
    {
        try
        {
            switch (number)
            {
                case 1:
                    Basic01_SimpleHistogram.Run();
                    break;
                case 2:
                    Basic02_HistogramWithTags.Run();
                    break;
                case 3:
                    Basic03_HistogramUnits.Run();
                    break;
                case 4:
                    Basic04_Percentiles.Run();
                    break;
                case 5:
                    Intermediate01_CustomBuckets.Run();
                    break;
                case 6:
                    Intermediate02_MultipleHistograms.Run();
                    break;
                case 7:
                    Intermediate03_TagCombinations.Run();
                    break;
                case 8:
                    Advanced01_InstrumentAdvice.Run();
                    break;
                case 9:
                    Advanced02_SloAlignedBuckets.Run();
                    break;
                case 10:
                    Advanced03_RequestLatencyScenario.Run();
                    break;
                case 11:
                    Advanced04_DatabaseQueryScenario.Run();
                    break;
                case 12:
                    Advanced05_OrderProcessingScenario.Run();
                    break;
                case 13:
                    Advanced06_BucketAlignmentImpact.Run();
                    break;
                default:
                    Console.WriteLine("Invalid example number.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running example: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
