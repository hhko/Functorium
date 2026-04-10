using QueryUsecaseExample;

var handler = new GetProductQuery.Handler();

var success = await handler.Handle(new GetProductQuery.Request("prod-001"), CancellationToken.None);
Console.WriteLine(success);

var fail = await handler.Handle(new GetProductQuery.Request("nonexistent"), CancellationToken.None);
Console.WriteLine(fail);
