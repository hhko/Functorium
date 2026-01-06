using OptionsPattern.Demo.Basic;
using OptionsPattern.Demo.Intermediate;
using OptionsPattern.Demo.Advanced;
using OptionsPattern.Demo.Production;

namespace OptionsPattern.Demo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    Options Pattern Tutorial                                  ║");
        Console.WriteLine("║              IOptions<T>, IOptionsSnapshot<T>, IOptionsMonitor<T>         ║");
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
            Console.WriteLine("BASIC LEVEL (IOptions<T>):");
            Console.WriteLine("  1. Basic01 - Simple Options");
            Console.WriteLine("  2. Basic02 - Options Registration Methods");
            Console.WriteLine("  3. Basic03 - AppSettings Binding");
            Console.WriteLine("  4. Basic04 - Options Validation");
            Console.WriteLine();
            Console.WriteLine("INTERMEDIATE LEVEL (IOptionsSnapshot<T>):");
            Console.WriteLine("  5. Intermediate01 - Options Snapshot");
            Console.WriteLine("  6. Intermediate02 - Scoped Options");
            Console.WriteLine("  7. Intermediate03 - Web Application Scenario");
            Console.WriteLine();
            Console.WriteLine("ADVANCED LEVEL (IOptionsMonitor<T>):");
            Console.WriteLine("  8. Advanced01 - Options Monitor");
            Console.WriteLine("  9. Advanced02 - Change Detection");
            Console.WriteLine(" 10. Advanced03 - Reload on Change");
            Console.WriteLine();
            Console.WriteLine("PRODUCTION LEVEL:");
            Console.WriteLine(" 11. Production01 - Configuration Reload ⭐");
            Console.WriteLine();
            Console.WriteLine("  0. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice: ");

            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input == "0")
                break;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 11)
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
                    Basic01_SimpleOptions.Run();
                    break;
                case 2:
                    Basic02_OptionsRegistration.Run();
                    break;
                case 3:
                    Basic03_AppSettingsBinding.Run();
                    break;
                case 4:
                    Basic04_OptionsValidation.Run();
                    break;
                case 5:
                    Intermediate01_OptionsSnapshot.Run();
                    break;
                case 6:
                    Intermediate02_ScopedOptions.Run();
                    break;
                case 7:
                    Intermediate03_WebAppScenario.Run();
                    break;
                case 8:
                    Advanced01_OptionsMonitor.Run();
                    break;
                case 9:
                    Advanced02_ChangeDetection.Run();
                    break;
                case 10:
                    Advanced03_ReloadOnChange.Run();
                    break;
                case 11:
                    Production01_ConfigReload.Run();
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
