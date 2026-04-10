using LanguageExt.Common;
using FinResponseFactoryCrtp;

// CRTP: CreateFail 호출
var fail = FactoryResponse<string>.CreateFail(Error.New("validation error"));
Console.WriteLine($"IsFail: {fail.IsFail}");

// Pipeline에서 사용
var result = ValidationPipelineExample.ValidateAndCreate(
    isValid: false,
    onSuccess: () => FactoryResponse<string>.Succ("OK"),
    errorMessage: "input is invalid");

Console.WriteLine($"Result.IsFail: {result.IsFail}");
