using FinResponseCovariant;

IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

// 공변성: IFinResponse<string> → IFinResponse<object>
IFinResponse<object> objectResponse = stringResponse;
Console.WriteLine($"objectResponse.IsSucc: {objectResponse.IsSucc}");

// 비제네릭 마커로도 대입 가능
IFinResponse nonGeneric = stringResponse;
Console.WriteLine($"nonGeneric.IsSucc: {nonGeneric.IsSucc}");
