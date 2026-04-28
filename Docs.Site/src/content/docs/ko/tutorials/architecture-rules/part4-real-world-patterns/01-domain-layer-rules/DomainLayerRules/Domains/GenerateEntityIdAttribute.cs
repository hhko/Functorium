namespace DomainLayerRules.Domains;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEntityIdAttribute : Attribute;
