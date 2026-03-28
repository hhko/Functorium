using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Intermediate;
using SpecificationPattern.Demo.Advanced;

namespace SpecificationPattern.Demo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   Specification Pattern Tutorial                           ║");
        Console.WriteLine("║            Repository + Specification + Expression 학습                    ║");
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
            Console.WriteLine("  1. Basic01 - Specification 정의와 평가");
            Console.WriteLine("  2. Basic02 - And, Or, Not 조합");
            Console.WriteLine("  3. Basic03 - 연산자(&, |, !)");
            Console.WriteLine();
            Console.WriteLine("INTERMEDIATE LEVEL:");
            Console.WriteLine("  4. Intermediate01 - All 항등원 + 동적 필터");
            Console.WriteLine("  5. Intermediate02 - Repository + Specification 연동");
            Console.WriteLine("  6. Intermediate03 - Expression 기반 Specification");
            Console.WriteLine();
            Console.WriteLine("ADVANCED LEVEL:");
            Console.WriteLine("  7. Advanced01 - 복합 Expression 해석");
            Console.WriteLine("  8. Advanced02 - 엔티티→모델 Expression 변환");
            Console.WriteLine();
            Console.WriteLine("  0. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice: ");

            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input == "0")
                break;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 8)
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
                    Basic01_SimpleSpec.Run();
                    break;
                case 2:
                    Basic02_Composition.Run();
                    break;
                case 3:
                    Basic03_Operators.Run();
                    break;
                case 4:
                    Intermediate01_AllIdentity.Run();
                    break;
                case 5:
                    Intermediate02_WithRepository.Run();
                    break;
                case 6:
                    Intermediate03_ExpressionSpec.Run();
                    break;
                case 7:
                    Advanced01_ExpressionResolver.Run();
                    break;
                case 8:
                    Advanced02_PropertyMap.Run();
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
