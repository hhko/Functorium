using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Functorium.Adapters.SourceGenerator.Abstractions;

public static class Selectors
{
    public static bool IsClass(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax;
    }
}
