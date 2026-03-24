namespace Functorium.Applications.Observabilities;

/// <summary>
/// ctx.* 프로퍼티의 관측 가능성 Pillar 타겟을 지정합니다.
/// 미지정 시 기본값은 <see cref="CtxPillar.Default"/> (Logging + Tracing)입니다.
/// 인터페이스에 적용하면 해당 인터페이스의 모든 프로퍼티에 일괄 적용되며,
/// 프로퍼티/파라미터 수준 지정이 인터페이스 수준보다 우선합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CtxTargetAttribute : Attribute
{
    public CtxPillar Pillars { get; }

    public CtxTargetAttribute(CtxPillar pillars)
    {
        Pillars = pillars;
    }
}
