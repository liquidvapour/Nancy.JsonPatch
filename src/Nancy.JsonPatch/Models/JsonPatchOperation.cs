namespace Nancy.JsonPatch.Models
{
    public class JsonPatchOperation
    {
        public JsonPatchOpCode Op { get; set; }
        public string Path { get; set; }
        public string From { get; set; }
        public object Value { get; set; }
    }
}