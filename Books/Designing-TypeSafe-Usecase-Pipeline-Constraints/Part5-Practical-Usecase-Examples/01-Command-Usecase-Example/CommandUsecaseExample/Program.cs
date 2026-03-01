using CommandUsecaseExample;

var handler = new CreateProductCommand.Handler();

var success = handler.Handle(new CreateProductCommand.Request("Widget", 9.99m));
Console.WriteLine(success);

var fail = handler.Handle(new CreateProductCommand.Request("", 9.99m));
Console.WriteLine(fail);
