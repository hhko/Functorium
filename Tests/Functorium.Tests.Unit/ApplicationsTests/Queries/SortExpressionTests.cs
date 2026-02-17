using Functorium.Applications.Queries;

namespace Functorium.Tests.Unit.ApplicationsTests.Queries;

public class SortExpressionTests
{
    [Fact]
    public void Empty_ReturnsEmptyExpression_WhenAccessed()
    {
        // Act
        var actual = SortExpression.Empty;

        // Assert
        actual.IsEmpty.ShouldBeTrue();
        actual.Fields.Count.ShouldBe(0);
    }

    [Fact]
    public void By_ReturnsSingleFieldExpression_WhenFieldNameProvided()
    {
        // Act
        var actual = SortExpression.By("Name");

        // Assert
        actual.IsEmpty.ShouldBeFalse();
        actual.Fields.Count.ShouldBe(1);
        actual.Fields[0].FieldName.ShouldBe("Name");
        actual.Fields[0].Direction.ShouldBe(Functorium.Applications.Queries.SortDirection.Ascending);
    }

    [Fact]
    public void By_ReturnsDescendingExpression_WhenDescendingDirectionProvided()
    {
        // Act
        var actual = SortExpression.By("Price", Functorium.Applications.Queries.SortDirection.Descending);

        // Assert
        actual.Fields[0].Direction.ShouldBe(Functorium.Applications.Queries.SortDirection.Descending);
    }

    [Fact]
    public void ThenBy_AppendsField_WhenCalledAfterBy()
    {
        // Act
        var actual = SortExpression.By("Name").ThenBy("Price", Functorium.Applications.Queries.SortDirection.Descending);

        // Assert
        actual.Fields.Count.ShouldBe(2);
        actual.Fields[0].FieldName.ShouldBe("Name");
        actual.Fields[0].Direction.ShouldBe(Functorium.Applications.Queries.SortDirection.Ascending);
        actual.Fields[1].FieldName.ShouldBe("Price");
        actual.Fields[1].Direction.ShouldBe(Functorium.Applications.Queries.SortDirection.Descending);
    }

    [Fact]
    public void ThenBy_PreservesFieldOrder_WhenMultipleFieldsAdded()
    {
        // Act
        var actual = SortExpression
            .By("A")
            .ThenBy("B")
            .ThenBy("C");

        // Assert
        actual.Fields.Count.ShouldBe(3);
        actual.Fields[0].FieldName.ShouldBe("A");
        actual.Fields[1].FieldName.ShouldBe("B");
        actual.Fields[2].FieldName.ShouldBe("C");
    }
}
