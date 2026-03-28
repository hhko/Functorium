namespace FinanceDomain.Tests.Unit;

/// <summary>
/// TransactionType 값 객체 테스트 (SmartEnum)
///
/// 학습 목표:
/// 1. SmartEnum 상수 값 검증
/// 2. 속성 검증 (IsCredit, IsDebit)
/// </summary>
[Trait("Part5-Finance-Domain", "TransactionTypeTests")]
public class TransactionTypeTests
{
    #region 상수 테스트

    [Fact]
    public void Deposit_IsCredit()
    {
        // Act & Assert
        TransactionType.Deposit.IsCredit.ShouldBeTrue();
        TransactionType.Deposit.IsDebit.ShouldBeFalse();
    }

    [Fact]
    public void Withdrawal_IsDebit()
    {
        // Act & Assert
        TransactionType.Withdrawal.IsCredit.ShouldBeFalse();
        TransactionType.Withdrawal.IsDebit.ShouldBeTrue();
    }

    [Fact]
    public void Transfer_IsDebit()
    {
        // Act & Assert
        TransactionType.Transfer.IsDebit.ShouldBeTrue();
    }

    [Fact]
    public void Interest_IsCredit()
    {
        // Act & Assert
        TransactionType.Interest.IsCredit.ShouldBeTrue();
    }

    [Fact]
    public void Fee_IsDebit()
    {
        // Act & Assert
        TransactionType.Fee.IsDebit.ShouldBeTrue();
    }

    #endregion

    #region 값 테스트

    [Fact]
    public void Deposit_HasCorrectValue()
    {
        // Act & Assert
        TransactionType.Deposit.Value.ShouldBe("DEPOSIT");
        TransactionType.Deposit.DisplayName.ShouldBe("입금");
    }

    [Fact]
    public void Withdrawal_HasCorrectValue()
    {
        // Act & Assert
        TransactionType.Withdrawal.Value.ShouldBe("WITHDRAWAL");
        TransactionType.Withdrawal.DisplayName.ShouldBe("출금");
    }

    #endregion

    #region List 테스트

    [Fact]
    public void List_ContainsAllTypes()
    {
        // Act
        var allTypes = TransactionType.List;

        // Assert
        allTypes.ShouldContain(TransactionType.Deposit);
        allTypes.ShouldContain(TransactionType.Withdrawal);
        allTypes.ShouldContain(TransactionType.Transfer);
        allTypes.ShouldContain(TransactionType.Interest);
        allTypes.ShouldContain(TransactionType.Fee);
        allTypes.Count().ShouldBe(5);
    }

    #endregion
}
