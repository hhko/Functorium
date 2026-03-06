using Functorium.Applications.Usecases;
using CreateOnlyConstraint;

var validationPipeline = new SimpleValidationPipeline<FinResponse<string>>();

var success = validationPipeline.Validate(
    isValid: true,
    onSuccess: () => FinResponse.Succ("Hello"));
Console.WriteLine(success);

var fail = validationPipeline.Validate(
    isValid: false,
    onSuccess: () => FinResponse.Succ("Hello"));
Console.WriteLine(fail);
