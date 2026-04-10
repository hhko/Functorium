using CommandUsecaseExample;

var handler = new CreateProductCommand.Handler();

var success = await handler.Handle(new CreateProductCommand.Request("Widget", 9.99m), CancellationToken.None);
Console.WriteLine(success);

var fail = await handler.Handle(new CreateProductCommand.Request("", 9.99m), CancellationToken.None);
Console.WriteLine(fail);
