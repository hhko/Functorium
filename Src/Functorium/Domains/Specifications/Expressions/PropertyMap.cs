using System.Linq.Expressions;
using System.Reflection;

namespace Functorium.Domains.Specifications.Expressions;

/// <summary>
/// Entity Expression → Model Expression 자동 변환을 위한 프로퍼티 매핑.
/// Entity 프로퍼티와 Model 프로퍼티 간의 매핑을 정의하고,
/// ExpressionVisitor를 사용하여 Expression Tree를 자동으로 변환합니다.
/// </summary>
/// <typeparam name="TEntity">도메인 엔터티 타입</typeparam>
/// <typeparam name="TModel">퍼시스턴스 모델 타입</typeparam>
public sealed class PropertyMap<TEntity, TModel>
{
    private readonly Dictionary<string, string> _mappings = new();

    /// <summary>
    /// Entity 프로퍼티와 Model 프로퍼티 간의 매핑을 등록합니다.
    /// </summary>
    /// <param name="entityProp">Entity 프로퍼티 접근 표현식 (예: p => (decimal)p.Price)</param>
    /// <param name="modelProp">Model 프로퍼티 접근 표현식 (예: m => m.Price)</param>
    public PropertyMap<TEntity, TModel> Map<TValue, TModelValue>(
        Expression<Func<TEntity, TValue>> entityProp,
        Expression<Func<TModel, TModelValue>> modelProp)
    {
        var entityMemberName = ExtractEntityMemberName(entityProp.Body);
        var modelMemberName = ExtractMemberName(modelProp.Body);
        _mappings[entityMemberName] = modelMemberName;
        return this;
    }

    /// <summary>
    /// 도메인 엔터티 필드명을 퍼시스턴스 모델 필드명으로 번역합니다.
    /// 매핑이 없으면 null을 반환합니다.
    /// </summary>
    public string? TranslateFieldName(string entityFieldName)
        => _mappings.TryGetValue(entityFieldName, out var modelFieldName) ? modelFieldName : null;

    /// <summary>
    /// Entity 기준 Expression을 Model 기준 Expression으로 변환합니다.
    /// </summary>
    public Expression<Func<TModel, bool>> Translate(Expression<Func<TEntity, bool>> expression)
    {
        var modelParam = Expression.Parameter(typeof(TModel), expression.Parameters[0].Name);
        var visitor = new TranslatingVisitor(expression.Parameters[0], modelParam, _mappings);
        var translatedBody = visitor.Visit(expression.Body);
        return Expression.Lambda<Func<TModel, bool>>(translatedBody, modelParam);
    }

    private static string ExtractEntityMemberName(Expression expression)
    {
        return expression switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression m } => m.Member.Name,
            MethodCallExpression { Method.Name: "ToString", Object: MemberExpression m } => m.Member.Name,
            _ => throw new ArgumentException(
                $"지원하지 않는 Entity 프로퍼티 표현식입니다: {expression}. " +
                "p => p.Property, p => (T)p.Property, p => p.Property.ToString() 형태만 지원합니다.")
        };
    }

    private static string ExtractMemberName(Expression expression)
    {
        return expression switch
        {
            MemberExpression m => m.Member.Name,
            _ => throw new ArgumentException(
                $"Model 프로퍼티는 단순 멤버 접근이어야 합니다: {expression}")
        };
    }

    /// <summary>
    /// Entity Expression을 Model Expression으로 변환하는 ExpressionVisitor.
    /// </summary>
    private sealed class TranslatingVisitor(
        ParameterExpression entityParam,
        ParameterExpression modelParam,
        Dictionary<string, string> mappings) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == entityParam ? modelParam : base.VisitParameter(node);

        protected override Expression VisitUnary(UnaryExpression node)
        {
            // Convert(entity.Property, targetType) → model.Property
            if (node.NodeType == ExpressionType.Convert &&
                node.Operand is MemberExpression { Expression: ParameterExpression param } member &&
                param == entityParam &&
                mappings.TryGetValue(member.Member.Name, out var modelPropertyName))
            {
                var modelProperty = Expression.Property(modelParam, modelPropertyName);
                if (modelProperty.Type == node.Type)
                    return modelProperty;
                return Expression.Convert(modelProperty, node.Type);
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // entity.Property.ToString() → model.Property
            if (node.Method.Name == "ToString" &&
                node.Arguments.Count == 0 &&
                node.Object is MemberExpression { Expression: ParameterExpression param } member &&
                param == entityParam &&
                mappings.TryGetValue(member.Member.Name, out var modelPropertyName))
            {
                var modelProperty = Expression.Property(modelParam, modelPropertyName);
                if (modelProperty.Type == node.Type)
                    return modelProperty;
                return Expression.Convert(modelProperty, node.Type);
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // entity.Property → model.Property (Convert 없이 직접 접근 시)
            if (node.Expression is ParameterExpression param &&
                param == entityParam &&
                mappings.TryGetValue(node.Member.Name, out var modelPropertyName))
            {
                return Expression.Property(modelParam, modelPropertyName);
            }

            return base.VisitMember(node);
        }
    }
}
