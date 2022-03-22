namespace Semgus.Operational {
    public class RecursionDepthException : Exception {
        public int Depth { get; }

        public RecursionDepthException(int depth) : base($"Exceeded max recursion depth (at {depth})") { 
            Depth = depth;
        }
    }
}