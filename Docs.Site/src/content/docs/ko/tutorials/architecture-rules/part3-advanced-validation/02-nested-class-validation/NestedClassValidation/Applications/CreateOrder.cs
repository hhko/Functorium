namespace NestedClassValidation.Applications;

public sealed class CreateOrder
{
    public sealed class Request
    {
        public string CustomerName { get; }
        public string ProductName { get; }

        private Request(string customerName, string productName)
        {
            CustomerName = customerName;
            ProductName = productName;
        }

        public static Request Create(string customerName, string productName)
            => new(customerName, productName);
    }

    public sealed class Response
    {
        public string OrderId { get; }
        public bool Success { get; }

        private Response(string orderId, bool success)
        {
            OrderId = orderId;
            Success = success;
        }

        public static Response Create(string orderId, bool success)
            => new(orderId, success);
    }
}
