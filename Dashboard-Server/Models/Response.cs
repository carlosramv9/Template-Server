namespace Dashboard_Server.Models
{
    public class DataResponse
    {
        public object Data { get; set; }
        public bool Status { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
        public bool Status { get; set; }
    }
}
