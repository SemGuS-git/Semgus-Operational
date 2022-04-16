
namespace Semgus.OrderSynthesis.SketchSyntax.Parsing {
    internal class SketchFileContent {
        public IReadOnlyList<IStatement> Contents { get; }
        public string? SketchVer { get; }
        public string? SrcFile { get; }
        public int? TimeTaken { get; }

        public SketchFileContent(IEnumerable<IStatement> contents, string? sketch_ver, string? src_file, int? time) {
            this.Contents = contents.ToList();
            SketchVer = sketch_ver;
            SrcFile = src_file;
            TimeTaken = time;
        }
    }
}