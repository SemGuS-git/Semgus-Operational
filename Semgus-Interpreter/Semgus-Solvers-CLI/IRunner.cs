namespace Semgus.CommandLineInterface {
    public interface IRunner {
        void Run(string inputFile);
        void Close();
    }
}