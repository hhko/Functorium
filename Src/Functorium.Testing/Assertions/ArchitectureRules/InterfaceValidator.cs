using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 인터페이스에 대한 아키텍처 규칙 검증을 수행하는 클래스입니다.
/// </summary>
public sealed class InterfaceValidator : TypeValidator<Interface, InterfaceValidator>
{
    public InterfaceValidator(Architecture architecture, Interface targetInterface)
        : base(architecture, targetInterface)
    {
    }

    protected override string TypeKind => "Interface";
}
