namespace Semgus.CommandLineInterface {
    public class InterpreterOutputRow {
        public string InterpreterLibVersion { get; set; }
        public string Semantics { get; set; }
        public string Program { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public double MeanRunTimeMS { get; set; }
    }
}