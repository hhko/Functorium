namespace NestedClassValidation.Applications;

public sealed class GetOrderById
{
    public sealed class Request
    {
        public string OrderId { get; }
        private Request(string orderId) => OrderId = orderId;
        public static Request Create(string orderId) => new(orderId);
    }

    public sealed class Response
    {
        public string OrderId { get; }
        public string CustomerName { get; }

        private Response(string orderId, string customerName)
        {
            OrderId = orderId;
            CustomerName = customerName;
        }

        public static Response Create(string orderId, string customerName)
            => new(orderId, customerName);
    }
}
