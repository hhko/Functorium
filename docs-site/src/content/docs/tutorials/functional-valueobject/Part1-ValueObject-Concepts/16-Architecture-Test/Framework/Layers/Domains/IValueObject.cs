namespace Framework.Layers.Domains;

public interface IValueObject
{
    const string CreateMethodName = "Create";
    const string CreateFromValidatedMethodName = "CreateFromValidated";
    const string ValidateMethodName = "Validate";

    const string DomainErrorsNestedClassName = "DomainErrors";
}
