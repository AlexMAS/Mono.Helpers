namespace System
{
    public class ProcessResult
    {
        public bool Completed { get; set; }

        public int? ExitCode { get; set; }

        public string Output { get; set; }
    }
}