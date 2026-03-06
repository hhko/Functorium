using QueryUsecaseExample;

var handler = new GetProductQuery.Handler();

var success = handler.Handle(new GetProductQuery.Request("prod-001"));
Console.WriteLine(success);

var fail = handler.Handle(new GetProductQuery.Request("nonexistent"));
Console.WriteLine(fail);
