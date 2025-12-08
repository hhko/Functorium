using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies;

// Field                | ErrorCodeExpected | ErrorCodeExpected<T>  | ErrorCodeExceptional  | ManyErrors
// ---                  | ---               | ---                   | ---                   | ---
// ErrorType            | O                 | O                     | O                     | O
// ErrorCode            | O                 | O                     | O                     | X
// ErrorCodeId          | O                 | O                     | O                     | O
// ErrorCurrentValue    | O                 | O                     | X                     | X
// Message              | O                 | O                     | O                     | X
// Count                | x                 | x                     | X                     | O
// Errors               | x                 | x                     | X                     | O (개별 타입)
// ExceptionDetails     | X                 | X                     | O(Serilog.Exceptions) | X

// -----------------------------------
// ErrorCodeExpected
// -----------------------------------
//"Properties": {
//    "Error": {
//        "ErrorType": "ErrorCodeExpected",         <- string 에러 타입
//        "ErrorCode": "<에러 코드>",
//        "ErrorCodeId": <에러 Id>
//        "ErrorCurrentValue": "<현재 값>"
//        "Message": "<에러 메시지>",
//    },

// -----------------------------------
// ErrorCodeExpected<T>
// -----------------------------------
//"Properties": {
//    "Error": {
//        "ErrorType": "ErrorCodeExpected`1",       <- <T> 에러 타입
//        "ErrorCode": "<에러 코드>",
//        "ErrorCodeId": <에러 Id>
//        "ErrorCurrentValue": {
//            "X": 2025,
//            "Y": 2026,
//            "_typeTag": "Foo"
//        },
//        "Message": "<에러 메시지>"
//    },

// -----------------------------------
// ErrorCodeExpected<T>
// -----------------------------------
//"Properties": {
//    "Error": {
//        "ErrorType": "ErrorCodeExpected`2",       <- <T1, T2> 에러 타입
//        "ErrorCode": "<에러 코드>",
//        "ErrorCodeId": <에러 Id>
//        "ErrorCurrentValue1": {
//            "X": 2025,
//            "Y": 2026,
//            "_typeTag": "Foo"
//        },
//        "ErrorCurrentValue2": {
//            "X": 2025,
//            "Y": 2026,
//            "_typeTag": "Foo"
//        },
//        "Message": "<에러 메시지>"
//    },

// -----------------------------------
// ManyErrors: Message을 생략하고, 개별 Error에서 메시지 제공
// -----------------------------------
//"Properties": {
//    "Error": {
//        "ErrorType": "ManyErrors",                <- 에러 타입
//        "ErrorCodeId": -2000000006,               <- -2000000006: ManyErrors 고유 Id
//        "Count": 2,                               <- 에러 건수
//        "Errors": [
//            {
//                "ErrorType": "ErrorCodeExpected",
//                "ErrorCode": "<에러 코드>",
//                "ErrorCodeId": <에러 Id>,
//                "ErrorCurrentValue": {
//                    "X": 2025,
//                    "Y": 2026,
//                    "_typeTag": "Foo"
//                },
//                "Message": "<에러 메시지>"
//            },
//            { ... 에러 코드... }
//        ]
//    },

// -----------------------------------
// ErrorCodeExceptional
// -----------------------------------
//"Properties": {
//    "Error": {
//        "ErrorType": "ErrorCodeExceptional",                  <- 예외 타입
//        "ErrorCodeId": -2147352558,                           <- Exception의 HResult
//        "Message": "Attempted to divide by zero.",            <- Exception의 Message
//        "ExceptionDetails": {
//            "TargetSite": "Int32 Divide(Int32, Int32)",
//            "Message": "Attempted to divide by zero.",
//            "Data": [],
//            "InnerException": null,
//            "HelpLink": null,
//            "Source": "GymManagement.Application",
//            "HResult": -2147352558,
//            "StackTrace": "   at GymManagement.Application.Usecases.Profiles.Queries.GetProfileQuery.Divide(Int32 x, Int32 y) in E:\\2025년\\Dev\\DDD-Course\\2025-06-23\\04-DddGym_Monolithic\\Backends\\GymManagement\\Src\\GymManagement.Application\\Usecases\\Profiles\\Queries\\GetProfileQuery.cs:line 124\r\n   at GymManagement.Application.Usecases.Profiles.Queries.GetProfileQuery.Usecase.Handle(Request request, CancellationToken cancellationToken) in E:\\2025년\\Dev\\DDD-Course\\2025-06-23\\04-DddGym_Monolithic\\Backends\\GymManagement\\Src\\GymManagement.Application\\Usecases\\Profiles\\Queries\\GetProfileQuery.cs:line 71",
//            "_typeTag": "DivideByZeroException"
//        }

public interface IErrorDestructurer
{
    bool CanHandle(Error error);
    LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory);
}
