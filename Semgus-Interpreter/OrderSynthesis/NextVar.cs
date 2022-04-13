#define INT_ATOM_FLAGS
#define INT_MONO_FLAGS


namespace Semgus.OrderSynthesis {
    internal class NextVar {
        public SketchLanguage.PrimitiveType Type;
        public string Name;

        public NextVar(SketchLanguage.PrimitiveType langPrim, string v) {
            this.Type = langPrim;
            this.Name = v;
        }

        public string Decl => $"{Extractor.Stringify(Type)} {Name}";
    }
}