using System.Text;

namespace Semgus.Interpretation {
    public interface ISmtLibExpression {
        //Type ResultType { get; }
        object Evaluate(EvaluationContext context);

        string PrettyPrint();
        void PrettyPrint(StringBuilder sb);
    }
}