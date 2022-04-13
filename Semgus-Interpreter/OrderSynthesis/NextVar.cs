#define INT_ATOM_FLAGS
#define INT_MONO_FLAGS


namespace Semgus.CommandLineInterface {
    internal class NextVar {
        public LangPrim Type;
        public string Name;

        public NextVar(LangPrim langPrim, string v) {
            this.Type = langPrim;
            this.Name = v;
        }

        public string Decl => $"{Extractor.Stringify(Type)} {Name}";
    }
}