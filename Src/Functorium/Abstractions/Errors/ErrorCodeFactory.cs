using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Abstractions.Errors;

public static class ErrorCodeFactory
{
    // ErrorCodeExpected
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create(string errorCode,
                               string errorCurrentValue,
                               string errorMessage) =>
        new ErrorCodeExpected(
            errorCode,
            errorCurrentValue,
            errorMessage);

    // ErrorCodeExpected<T>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T>(string errorCode,
                                  T errorCurrentValue,
                                  string errorMessage) where T : notnull =>
        new ErrorCodeExpected<T>(
            errorCode,
            errorCurrentValue,
            errorMessage);

    // ErrorCodeExpected<T1, T2>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2>(string errorCode,
                                       T1 errorCurrentValue1,
                                       T2 errorCurrentValue2,
                                       string errorMessage) where T1 : notnull where T2 : notnull =>
        new ErrorCodeExpected<T1, T2>(
            errorCode,
            errorCurrentValue1,
            errorCurrentValue2,
            errorMessage);

    // ErrorCodeExpected<T1, T2, T3>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2, T3>(string errorCode,
                                           T1 errorCurrentValue1,
                                           T2 errorCurrentValue2,
                                           T3 errorCurrentValue3,
                                           string errorMessage) where T1 : notnull where T2 : notnull where T3 : notnull =>
        new ErrorCodeExpected<T1, T2, T3>(
            errorCode,
            errorCurrentValue1,
            errorCurrentValue2,
            errorCurrentValue3,
            errorMessage);

    // ErrorCodeExceptional
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateFromException(string errorCode,
                                            Exception exception) =>
        new ErrorCodeExceptional(
            errorCode,
            exception);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(params string[] parts) =>
        string.Join('.', parts);
}


//// DomainErrorSourceGenerator.cs
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Text;
//using System.Text;

//namespace DomainErrorGenerator
//{
//    [Generator]
//    public class DomainErrorSourceGenerator : ISourceGenerator
//    {
//        public void Initialize(GeneratorInitializationContext context)
//        {
//            context.RegisterForSyntaxNotifications(() => new ErrorMethodSyntaxReceiver());
//        }

//        public void Execute(GeneratorExecutionContext context)
//        {
//            if (context.SyntaxReceiver is not ErrorMethodSyntaxReceiver receiver)
//                return;

//            foreach (var candidate in receiver.CandidateMethods)
//            {
//                var classNode = candidate.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
//                var parentClass = classNode?.Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
//                if (classNode == null || parentClass == null) continue;

//                string classNamespace = GetNamespace(classNode);
//                string parentClassName = parentClass.Identifier.Text;
//                string className = classNode.Identifier.Text;
//                string methodName = candidate.Identifier.Text;

//                var parameters = candidate.ParameterList.Parameters;
//                var parameterList = string.Join(", ", parameters.Select(p => $"{p.Type} {p.Identifier}"));

//                var errorCode = $"\"{parentClassName}.{className}.{methodName}\"";

//                var errorValue = string.Join(", ", parameters.Select(p => $"{p.Identifier.Text}: \" + {p.Identifier.Text} + \""));

//                var messageArguments = string.Join(", ", parameters.Select(p => $"'" + " + {p.Identifier.Text} + "'"));

//                var message = methodName switch
//                {
//                    "ReservationInPast" => $"A participant cannot cancel the reservation '{{{parameters[1].Identifier}}}' for a session '{{{parameters[0].Identifier}}}' that has completed '{{{parameters[2].Identifier}}}'",
//                    "GymAlreadyExist" => $"Subscription '{{{parameters[0].Identifier}}}' already has a gym '{{{parameters[1].Identifier}}}'",
//                    _ => "Error occurred with parameters: " + errorValue
//                };

//                var source = $@"
//namespace {classNamespace}
//{{
//    public static partial class {parentClassName}
//    {{
//        public static partial class {className}
//        {{
//            public static Error {methodName}({parameterList}) =>
//                ErrorCodeFactory.Create(
//                    errorCode: $"{ { nameof({ parentClassName})} }.{ { nameof({ className})} }.{ { nameof({ methodName})} }
//                ",
//                    errorValue: $"{errorValue}",
//                    errorMessage: $"{message}");
//            }
//        }
//    }
//}
//}}
//";
//                context.AddSource($"{methodName}Error.g.cs", SourceText.From(source, Encoding.UTF8));
//            }
//        }

//        private static string GetNamespace(SyntaxNode node)
//{
//    while (node != null)
//    {
//        if (node is NamespaceDeclarationSyntax namespaceDeclaration)
//            return namespaceDeclaration.Name.ToString();
//        node = node.Parent;
//    }
//    return "Global";
//}

//class ErrorMethodSyntaxReceiver : ISyntaxReceiver
//{
//    public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

//    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
//    {
//        if (syntaxNode is MethodDeclarationSyntax method && method.Body == null && method.Modifiers.Any(SyntaxKind.PartialKeyword))
//        {
//            CandidateMethods.Add(method);
//        }
//    }
//}
//    }
//}

//// 사용 예시 (정의 코드)
//// public static partial class DomainErrors
//// {
////     public static partial class SessionErrors
////     {
////         public static partial Error ReservationInPast(Guid sessionId, DateTime reservationDateTime, DateTime utcNow);
////     }
//// }
