namespace Functorium.Adapters.SourceGenerator;

/// <summary>
/// IAdapter 인터페이스를 구현하는 클래스에 이 속성을 적용하면
/// 해당 클래스의 Pipeline 버전이 자동으로 생성됩니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GeneratePipelineAttribute : Attribute
{
    public GeneratePipelineAttribute()
    {
    }
}