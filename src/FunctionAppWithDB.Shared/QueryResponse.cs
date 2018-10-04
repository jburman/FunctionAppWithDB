namespace FunctionAppWithDB.Shared
{
    public class QueryResponse<T>
    {
        public QueryResponse()
        {
        }

        public QueryResponse(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}
