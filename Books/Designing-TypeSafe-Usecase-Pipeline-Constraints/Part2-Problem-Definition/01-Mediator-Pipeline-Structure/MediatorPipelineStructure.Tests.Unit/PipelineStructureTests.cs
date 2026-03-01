namespace MediatorPipelineStructure.Tests.Unit;

public class PipelineStructureTests
{
    [Fact]
    public void ConstrainedPipeline_CanAccessIsSuccess_WhenTResponseImplementsIResult()
    {
        // Arrange & Act - just verify the type constraint compiles
        var pipeline = new ConstrainedPipeline<TestRequest, TestResponse>();

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    public void SimpleLoggingPipeline_HasEmptyLogs_WhenCreated()
    {
        // Arrange & Act
        var pipeline = new SimpleLoggingPipeline<TestRequest, TestResponse>();

        // Assert
        pipeline.Logs.ShouldBeEmpty();
    }
}

// Test helper types
file record TestRequest : Mediator.IRequest<TestResponse>;
file record TestResponse(bool IsSuccess) : IResult;
