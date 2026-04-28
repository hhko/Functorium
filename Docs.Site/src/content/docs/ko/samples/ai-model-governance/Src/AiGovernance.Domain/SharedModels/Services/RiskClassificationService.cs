using Functorium.Domains.Errors;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using static Functorium.Domains.Errors.DomainErrorKind;

namespace AiGovernance.Domain.SharedModels.Services;

/// <summary>
/// AI 모델 위험 등급 분류 도메인 서비스.
/// 모델 목적(purpose) 키워드 기반으로 위험 등급을 분류합니다.
/// </summary>
public sealed class RiskClassificationService : IDomainService
{
    private static readonly string[] UnacceptableKeywords =
        ["social scoring", "real-time surveillance"];

    private static readonly string[] HighKeywords =
        ["hiring", "credit", "medical", "biometric"];

    private static readonly string[] LimitedKeywords =
        ["sentiment", "recommendation", "emotion"];

    /// <summary>
    /// 모델 목적 키워드 기반으로 위험 등급을 분류합니다.
    /// </summary>
    public Fin<RiskTier> ClassifyByPurpose(ModelPurpose purpose)
    {
        string purposeLower = ((string)purpose).ToLowerInvariant();

        if (UnacceptableKeywords.Any(k => purposeLower.Contains(k)))
            return RiskTier.Unacceptable;

        if (HighKeywords.Any(k => purposeLower.Contains(k)))
            return RiskTier.High;

        if (LimitedKeywords.Any(k => purposeLower.Contains(k)))
            return RiskTier.Limited;

        return RiskTier.Minimal;
    }
}
