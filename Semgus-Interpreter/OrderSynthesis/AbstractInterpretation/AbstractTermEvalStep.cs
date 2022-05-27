using Semgus.Operational;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal record AbstractTermEvalStep(TermEvaluation src, int NodeTermIndex, int InputTupleIndex, int OutputTupleIndex);
}
