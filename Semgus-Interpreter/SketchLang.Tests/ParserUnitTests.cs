using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Sprache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Semgus.SketchLang.Tests {

    [TestClass]
    public class ParserUnitTests {
        static void AssertEmpty<T>(IEnumerable<T> val) {

            var list = val.ToList();

            if (list.Count > 0) {
                StringBuilder sb = new();
                void ErrRecurseDump(object value) {
                    if (value is IEnumerable en) {
                        bool o = false;
                        sb.Append(value.GetType().Name);
                        sb.Append('[');
                        foreach (var a in en) {
                            if (o) sb.Append(", ");
                            else o = true;
                            ErrRecurseDump(a);
                        }
                        sb.Append(']');
                    } else {
                        sb.Append(value.ToString());
                    }
                }
                ErrRecurseDump(list);
                throw new AssertFailedException($"AssertEmpty failed. Values:{sb}");
            }
        }


        [DataTestMethod]
        [DataRow("33", 33)]
        [DataRow("0", 0)]
        [DataRow("1050", 1050)]
        public void TestParseNonNegativeNumber(string str, int value) {
            Assert.AreEqual(value, SketchParser.NonNegativeNumber.Parse(str));
        }

        [DataTestMethod]
        [DataRow("03", -123)]
        [DataRow("00", -123)]
        [DataRow("", -123)]
        [DataRow("a12", -123)]
        [DataRow("a0", -123)]
        [DataRow("0a", -123)]
        [DataRow("1a", -123)]
        [DataRow("1_", -123)]
        [DataRow("_1", -123)]
        [DataRow("0_", -123)]
        [DataRow("_0", -123)]
        public void TestParseOneNonNegativeNumberFails(string str, int neq_value) {
            Assert.AreEqual(neq_value, SketchParser.NonNegativeNumber.Optional().Parse(str).GetOrElse(neq_value));
        }



        /***** Literal ******/
        static IEnumerable<object[]> LiteralCases => new[] {
            new object[] { "   1  ", new Literal(1) },
            new object[] { "30", new Literal(30) },
            new object[] { "/*etc*/ 3 //a", new Literal(3) },
        };

        [DataTestMethod]
        [DynamicData(nameof(LiteralCases))]
        public void TestParseLiteral(string str, object value) {
            Assert.AreEqual(value, SketchParser.Literal.Parse(str));
        }


        [DataTestMethod]
        [DataRow("-5")]
        [DataRow("5_0")]
        [DataRow("05")]
        public void TestParseOneLiteralFails(string str) {
            Assert.AreEqual(default(Literal), SketchParser.Literal.Optional().Parse(str).GetOrDefault());
        }




        static IEnumerable<object[]> IdentifierCases => new[] {
            new object[] { " _  ", new Identifier("_") },
            new object[] { " _0  ", new Identifier("_0") },
            new object[] { " _a ", new Identifier("_a") },
            new object[] { "Az0_2", new Identifier("Az0_2") },
            new object[] { "Out_1@ANONYMOUS", new Identifier("Out_1@ANONYMOUS") },
            new object[] { "/*etc*/qr87/*weird*/ //a", new Identifier("qr87") }
        };

        [DataTestMethod]
        [DynamicData(nameof(IdentifierCases))]
        public void TestParseIdentifier(string str, object value) {
            Assert.AreEqual(value, SketchParser.Identifier.Parse(str));
        }

        [DataTestMethod]
        [DataRow("-2")]
        [DataRow("0a")]
        [DataRow("return")]
        [DataRow("assert")]
        [DataRow(" if ")]
        [DataRow("  else if")]
        [DataRow("  else if")]
        public void TestParseIdentifierFails(string str) {
            AssertEmpty(SketchParser.Identifier.Many().Parse(str));
        }



        /***** Hole ******/

        static IEnumerable<object[]> HoleCases => new[] {
            new object[] { "  ?? ", new Hole() }
        };

        [DataTestMethod]
        [DynamicData(nameof(HoleCases))]
        public void TestParseHole(string str, object value) {
            Assert.AreEqual(value, SketchParser.Hole.Parse(str));
        }

        [DataTestMethod]
        [DataRow("?")]
        [DataRow("4??")]
        [DataRow("?e?")]
        [DataRow("_??")]
        public void TestParseHoleFails(string str) {
            AssertEmpty(SketchParser.Hole.Many().Parse(str));
        }



        /***** VariableRef ******/
        static IEnumerable<object[]> VariableRefCases => new[] {
            new object[] { "  something ", Var("something") }
        };

        [DataTestMethod]
        [DynamicData(nameof(VariableRefCases))]
        public void TestParseVariableRef(string str, object value) {
            Assert.AreEqual(value, SketchParser.VariableRef.Parse(str));
        }

        [DataTestMethod]
        [DataRow("return")]
        [DataRow("123")]
        public void TestParseVariableRefFails(string str) {
            AssertEmpty(SketchParser.VariableRef.Many().Parse(str));
        }



        /***** Expression ******/
        static IEnumerable<object[]> ExpressionCases => new[] {
            new object[] { "??", new Hole() },
            new object[] { "value", Var("value") },
            new object[] { "value.v0", Var("value").Get(new("v0")) },
            new object[] { "-x", UnaryOp.Minus.Of(Var("x")) },
            new object[] { "-5", UnaryOp.Minus.Of(Lit(5)) },
            new object[] { "-(5)", UnaryOp.Minus.Of(Lit(5))},
            new object[] { "-(-5)", UnaryOp.Minus.Of(UnaryOp.Minus.Of(Lit(5)))},
        };

        [DataTestMethod]
        [DynamicData(nameof(ExpressionCases))]
        public void TestParseExpression(string str, object value) {
            Assert.AreEqual(value, SketchParser.Expression.Parse(str));
        }

        [DataTestMethod]
        [DataRow("return;")]
        [DataRow("5a")]
        public void TestParseOneExpressionFails(string str) {
            Assert.ThrowsException<ParseException>(() => SketchParser.Expression.Parse(str));
        }



        /***** PropertyAccess ******/
        static IEnumerable<object[]> PropertyAccessCases => new[] {
            new object[] { "value.v0",  new PropertyAccess(Var("value"),new("v0")) },
            new object[] { "value /*comment*/. // comment\n v0", Var("value").Get(new("v0")) },
            new object[] { "value.v0._a", Var("value").Get(new("v0")).Get(new("_a")) }
        };

        [DataTestMethod]
        [DynamicData(nameof(PropertyAccessCases))]
        public void TestParsePropertyAccess(string str, object value) {
            Assert.AreEqual(value, SketchParser.PropertyAccess.Parse(str));
        }

        [DataTestMethod]
        [DataRow("value")]
        [DataRow(" .v0")]
        [DataRow("value..v0")]
        public void TestParsePropertyAccessFails(string str) {
            AssertEmpty(SketchParser.PropertyAccess.Many().Parse(str));
        }




        /***** ISettable ******/
        static IEnumerable<object[]> SettableCases => new[] {
            new object[] { "value", Var("value") },
            new object[] { "value.v0", Var("value").Get(new("v0")) }
        };

        [DataTestMethod]
        [DynamicData(nameof(SettableCases))]
        public void TestParseSettable(string str, object value) {
            Assert.AreEqual(value, SketchParser.Settable.Parse(str));
        }

        [DataTestMethod]
        [DataRow("return")]
        public void TestParseSettableFails(string str) {
            AssertEmpty(SketchParser.Settable.Many().Parse(str));
        }




        /***** WeakVariableDeclaration ******/
        static IEnumerable<object[]> WeakVariableDeclarationCases => new[] {
            new object[] { "int x", new WeakVariableDeclaration(new("int"),new("x")) },
            new object[] { "int x = 2", new WeakVariableDeclaration(new("int"),new("x"),new Literal(2)) },
            new object[] { "int/*test*/x/*test*/=/*test*/2", new WeakVariableDeclaration(new("int"),new("x"),new Literal(2)) },
            new object[] { "int//a\nx//a\n=//a\n2", new WeakVariableDeclaration(new("int"),new("x"),new Literal(2)) }
        };

        [DataTestMethod]
        [DynamicData(nameof(WeakVariableDeclarationCases))]
        public void TestParseWeakVariableDeclaration(string str, object value) {
            Assert.AreEqual(value, SketchParser.WeakVariableDeclaration.Parse(str));
        }

        [DataTestMethod]
        [DataRow("assert x")]
        [DataRow("123 x")]
        [DataRow("int x.y")]
        [DataRow("int = 5")]
        //[DataRow("int x + 1")]
        public void TestParseWeakVariableDeclarationFails(string str) {
            AssertEmpty(SketchParser.WeakVariableDeclaration.Many().Parse(str));
        }


        /***** FunctionArg ******/
        static IEnumerable<object[]> FunctionArgCases => new[] {
            new object[] { "int x", new WeakVariableDeclaration(new("int"),new("x")) },
            new object[] { "ref int x", new RefVariableDeclaration(new WeakVariableDeclaration(new("int"), new("x"))) },
            new object[] { "ref/*a*/ //b\nint/*a*/ //b\nx", new RefVariableDeclaration(new WeakVariableDeclaration(new("int"), new("x"))) }
        };

        [DataTestMethod]
        [DynamicData(nameof(FunctionArgCases))]
        public void TestParseFunctionArg(string str, object value) {
            Assert.AreEqual(value, SketchParser.FunctionArg.Parse(str));
        }

        [DataTestMethod]
        [DataRow("value")]
        [DataRow("ref x")]
        [DataRow("ref int x.y")]
        [DataRow("ref int.a x")]
        [DataRow("ref x = 5")]
        [DataRow("x = 5")]
        public void TestParseFunctionArgFails(string str) {
            AssertEmpty(SketchParser.FunctionArg.Many().Parse(str));
        }




        /***** FunctionArgList ******/
        static IEnumerable<object[]> FunctionArgListCases => new[] {
            new object[] { "()", new object[] { } },
            new object[] { "(\n)", new object[] { } },
            new object[] { "( // none\n)", new object[] { } },
            new object[] { "(int x,\n ref int y)", new object[] {
                VDec("int", "x"),
                new RefVariableDeclaration(VDec("int", "y"))
            } },
        };

        [DataTestMethod]
        [DynamicData(nameof(FunctionArgListCases))]
        public void TestParseFunctionArgList(string str, ICollection value) {
            CollectionAssert.AreEqual(value, SketchParser.FunctionArgList.Parse(str).ToList());
        }

        [DataTestMethod]
        [DataRow("(pattern)")]
        [DataRow("(ok ok ok)")]
        [DataRow("(ref ref int x)")]
        [DataRow("(,int x)")]
        [DataRow("(int x,)")]
        [DataRow("(int x = 5)")]
        [DataRow("int x")]
        public void TestParseFunctionArgListFails(string str) {
            AssertEmpty(SketchParser.FunctionArgList.Many().Parse(str));
        }




        /***** WeakFunctionSignature ******/
        static IEnumerable<object[]> WeakFunctionSignatureCases => new[] {
            new object[] { " int mul (int x, int y, ref bit flag)", new WeakFunctionSignature(
                FunctionModifier.None, new("int"), new("mul"), new IVariableInfo[] {
                    VDec("int", "x"),
                    VDec("int", "y"),
                    new RefVariableDeclaration(VDec("bit", "flag"))
                })
            },
            new object[] { " generator bit t(\n)", new WeakFunctionSignature(
                FunctionModifier.Generator, new("bit"), new("t"), Array.Empty<IVariableInfo>() )
            },
            new object[] { " generator bit t(\n) implements __mu_iota", new WeakFunctionSignature(
                FunctionModifier.Generator, new("bit"), new("t"), Array.Empty<IVariableInfo>()
                ){ImplementsId=new("__mu_iota")}
            },
        };

        [DataTestMethod]
        [DynamicData(nameof(WeakFunctionSignatureCases))]
        public void TestParseWeakFunctionSignature(string str, object value) {
            var res = SketchParser.WeakFunctionSignature.Parse(str);
            Assert.AreEqual(value, res);
        }

        [DataTestMethod]
        [DataRow("generator mul()")]
        [DataRow("other valuable mul()")]
        [DataRow("non sequitur")]
        public void TestParseWeakFunctionSignatureFails(string str) {
            AssertEmpty(SketchParser.WeakFunctionSignature.Many().Parse(str));
        }




        /***** ProceduralBlock ******/
        static IEnumerable<object[]> ProceduralBlockCases => new[] {
            new object[] { "{}", Array.Empty<IStatement>() },
            new object[] { "{\nint x = 5;\n}", new IStatement[]{
                VDec("int", "x", Lit(5))
            }},
            new object[] { "{\nint x = 5; return x;\n}", new IStatement[]{
                VDec("int", "x", Lit(5)),
                new ReturnStatement(Var("x"))
            }},
            new object[] { "{\nint x = 5;x = y;\n}", new IStatement[]{
                VDec("int", "x", Lit(5)),
                Var("x").Assign(Var("y"))
            }},
            new object[] { "{\nint x = 5;\nrepeat(??) { } x = y; return x;\n}", new IStatement[]{
                VDec("int", "x", Lit(5)),
                new RepeatStatement(new Hole()),
                Var("x").Assign(Var("y")),
                new ReturnStatement(Var("x"))
            }}
        };

        [DataTestMethod]
        [DynamicData(nameof(ProceduralBlockCases))]
        public void TestParseProceduralBlock(string str, ICollection value) {
            var list = SketchParser.ProceduralBlock.Parse(str).ToList();
            CollectionAssert.AreEqual(value, list,
                $"\n\texpected:{{{string.Join(" | ", (IEnumerable<IStatement>)value)}}},\n\tactual:{{{string.Join(" | ", list)}}}\n");
        }

        [DataTestMethod]
        [DataRow("{ int x = 5 }")]
        [DataRow("int x = 5;")]
        public void TestParseProceduralBlockFails(string str) {
            AssertEmpty(SketchParser.ProceduralBlock.Many().Parse(str));
        }



        /***** WrappedExpression ******/
        static IEnumerable<object[]> WrappedExpressionCases => new[] {
            new object[] { "(a)", Var("a") },
            new object[] { "((a))", Var("a") },
            new object[] { "(((a)))", Var("a") },
            new object[] { "((1) >= 2)", Op.Geq.Of(new Literal(1), new Literal(2)) },
            new object[] { "((?? ? 1 : 2) >= 1)",
                    Op.Geq.Of(
                        new Ternary(new Hole(), new Literal(1), new Literal(2)),
                        new Literal(1)
                    )
            }
        };

        [DataTestMethod]
        [DynamicData(nameof(WrappedExpressionCases))]
        public void TestParseWrappedExpression(string str, object value) {
            Assert.AreEqual(value, SketchParser.WrappedExpression.Parse(str));
        }

        [DataTestMethod]
        [DataRow("()")]
        [DataRow("a")]
        [DataRow("(()")]
        [DataRow("())")]
        public void TestParseWrappedExpressionFails(string str) {
            AssertEmpty(SketchParser.WrappedExpression.Many().Parse(str));
        }



        /***** InfixSequence ******/
        static IEnumerable<object[]> InfixSequenceCases { get; } = new[] {
            new object[] { "a < (b)", Op.Lt.Of(Var("a"),Var("b")) },
            new object[] { "((a)) < ((b))", Op.Lt.Of(Var("a"),Var("b")) },
            new object[] { "a + -b", Op.Plus.Of(Var("a"),UnaryOp.Minus.Of(Var("b")))},
            new object[] { "x.y < ((?? ? 1 : 2) >= (x > 10))",
                Op.Lt.Of(
                    Var("x").Get(new("y")),
                    Op.Geq.Of(
                        new Ternary(new Hole(), new Literal(1), new Literal(2)),
                        Op.Gt.Of(
                            Var("x"),
                            new Literal(10)
                        )
                    )
               )
            }

        }.Concat(
            new (int numReps, int numOps)[] {
                (8,5),
                (4,10),
                (2,20),
                (1,40)
            }.SelectMany(t => Enumerable.Repeat(t.numOps, t.numReps))
            .Select((numOps, i) => GenInfix(i, numOps)).Select(Exemplify)
        );

        [DataTestMethod]
        [DynamicData(nameof(InfixSequenceCases))]
        public void TestParseInfixSequence(string str, object value) {
            Assert.AreEqual(value, SketchParser.InfixSequence.Parse(str));
        }

        [DataTestMethod]
        [DataRow("a +")]
        [DataRow("- b")]
        [DataRow("a b")]
        [DataRow("a -- b")]
        [DataRow("a ++ b")]
        public void TestParseInfixSequenceFails(string str) {
            
            AssertEmpty(SketchParser.InfixSequence.Many().Parse(str));
        }




        /***** Ternary ******/
        static IEnumerable<object[]> TernaryCases => new[] {
            new object[] { "a ? b : c", new Ternary(Var("a"), Var("b"), Var("c")) },
            new object[] { "?? ? ?? : ??", new Ternary(new Hole(), new Hole(), new Hole()) },
            new object[] { "1 + 1 ? 1 + 1 : 1 + 1", new Ternary(Op.Plus.Of(new Literal(1), new Literal(1)), Op.Plus.Of(new Literal(1), new Literal(1)), Op.Plus.Of(new Literal(1), new Literal(1))) }
        };

        [DataTestMethod]
        [DynamicData(nameof(TernaryCases))]
        public void TestParseTernary(string str, object value) {
            Assert.AreEqual(value, SketchParser.Ternary.Parse(str));
        }

        [DataTestMethod]
        [DataRow("a ?? b : c")]
        public void TestParseTernaryFails(string str) {
            AssertEmpty(SketchParser.Ternary.Many().Parse(str));
        }








        /***** StructDefinition ******/
        static IEnumerable<object[]> StructDefinitionCases => new[] {

            new object[] {@"
// E outputs: ((Int r))
struct Out_0 {
    int v0;
}"
            , new StructDefinition(new("Out_0"), VDec("int","v0"))
            }
        };

        [DataTestMethod]
        [DynamicData(nameof(StructDefinitionCases))]
        public void TestParseStructDefinition(string str, object value) {
            var v = SketchParser.StructDefinition.Parse(str);

            if (!v.Equals(value)) {
                int x = 5;
            } 

            Assert.AreEqual(value, v);
        }

        [DataTestMethod]
        [DataRow("struct A")]
        public void TestParseStructDefinitionFails(string str) {
            AssertEmpty(SketchParser.StructDefinition.Many().Parse(str));
        }




        /***** RepeatStatement ******/
        static IEnumerable<object[]> RepeatStatementCases => new[] {
            new object[] { "repeat(??) { }", new RepeatStatement(new Hole()) },
            Exemplify(new RepeatStatement(Op.Plus.Of(Var("a"), Var("a")), new AssertStatement(Lit(1))))
        };

        [DataTestMethod]
        [DynamicData(nameof(RepeatStatementCases))]
        public void TestParseRepeatStatement(string str, object value) {
            Assert.AreEqual(value, SketchParser.RepeatStatement.Parse(str));
        }

        [DataTestMethod]
        [DataRow("repeat")]
        [DataRow("repeat() { }")]
        [DataRow("repeat(??)")]
        public void TestParseRepeatStatementFails(string str) {
            AssertEmpty(SketchParser.RepeatStatement.Many().Parse(str));
        }






        /***** IfStatement ******/
        static IEnumerable<object[]> IfStatementCases => new[] {
            new object[] { "if(x) {}", X.If(Var("x")) },
            new object[] { "if(x==5) {}", Var("x").IfEq(Lit(5)) },
            new object[] { "if(x==5) { assert(2); }", Var("x").IfEq(Lit(5), X.Assert(2)) },
            new object[] { "if(!(_pac_sc_s54)){ }/*ord-max..exp.sl.sk:206*/", X.If(UnaryOp.Not.Of(Var("_pac_sc_s54"))) },
            new object[] { "if(a) { } else { return; }", X.If(Var("a")).Else(X.Return()) },
            new object[] { "if(a) return; else return;", X.If(Var("a"), X.Return()).Else(X.Return()) },
            new object[] { "if(a) if(b) return 1; else return 2;",
                X.If(Var("a"),
                    X.If(Var("b"),
                        X.Return(1)
                    )
                    .Else(
                        X.Return(2)
                    )
                ),
            },
            new object[] { "if(a) { if(b) return 1; } else return 2;",
                X.If(Var("a"),
                    X.If(Var("b"),
                        X.Return(1)
                    )
                )
                .Else(
                    X.Return(2)
                )
            },
            new object[] { "if(a) return 1; else if(b) return 2; else return 3;",
                X.If(Var("a"),
                    X.Return(1)
                )
                .ElseIf(Var("b"),
                    X.Return(2)
                )
                .Else(
                    X.Return(3)
                )
            }
        };

        [DataTestMethod]
        [DynamicData(nameof(IfStatementCases))]
        public void TestParseIfStatement(string str, object value) {
            Assert.AreEqual(value, SketchParser.IfStatement.Parse(str));
        }

        [DataTestMethod]
        [DataRow("if(){}")]
        [DataRow("if(x +){}")]
        [DataRow("if(x)")]
        [DataRow("if(x) else {}")]
        public void TestParseIfStatementFails(string str) {
            AssertEmpty(SketchParser.IfStatement.Many().Parse(str));
        }





        /***** UnaryOperation ******/
        static IEnumerable<object[]> UnaryOperationCases => new[] {
            new object[] { "!x", UnaryOp.Not.Of(Var("x")) },
            new object[] { "!x", UnaryOp.Not.Of(Var("x")) },
            new object[] { "-x", UnaryOp.Minus.Of(Var("x")) },
            new object[] { "-5", UnaryOp.Minus.Of(Lit(5)) },
            new object[] { "-(-5)",UnaryOp.Minus.Of(UnaryOp.Minus.Of(Lit(5))) },
        };

        [DataTestMethod]
        [DynamicData(nameof(UnaryOperationCases))]
        public void TestParseUnaryOperation(string str, object value) {
            Assert.AreEqual(value, SketchParser.UnaryOperation.Parse(str));
        }

        [DataTestMethod]
        [DataRow("-")]
        [DataRow("!")]
        [DataRow("--x")]
        [DataRow("--5")]
        [DataRow("!!x")]
        public void TestParseUnaryOperationFails(string str) {
            AssertEmpty(SketchParser.UnaryOperation.Many().Parse(str));
        }





        /***** StructNew ******/
        static IEnumerable<object[]> StructNewCases => new[] {
            new object[] { "new obj(v0=x)", new StructNew(new("obj"),Var("v0").Assign(Var("x"))) }
        };

        [DataTestMethod]
        [DynamicData(nameof(StructNewCases))]
        public void TestParseStructNew(string str, object value) {
            Assert.AreEqual(value, SketchParser.StructNew.Parse(str));
        }

        [DataTestMethod]
        [DataRow("new obj")]
        [DataRow("obj(v0=x)")]
        [DataRow("new obj(5)")]
        public void TestParseStructNewFails(string str) {
            AssertEmpty(SketchParser.StructNew.Many().Parse(str));
        }



        /***** FunctionDefinition ******/
        static IEnumerable<object[]> FunctionDefinitionCases => new[] {
            new object[] { @"


bit compare_Out_0 (Out_0 a, Out_0 b) //ok
{
    bit leq = 0;
    repeat(??) {
        leq = leq || disjunct_Out_0(a, b);
    }
    return leq;
}


",
                new FunctionDefinition(
                    new WeakFunctionSignature(
                        FunctionModifier.None,
                        new("bit"),
                        new("compare_Out_0"),
                        new[]{VDec("Out_0","a"),VDec("Out_0","b") }
                    ),
                    VDec("bit","leq",Lit(0)),
                    new RepeatStatement(new Hole(),
                        Var("leq").Assign(Op.Or.Of(
                            Var("leq"),
                            new FunctionEval(new("disjunct_Out_0"),Var("a"),Var("b"))
                        ))
                    ),
                    new ReturnStatement(Var("leq"))
                )
            },
            new object[] { @"
harness void main() /*test*/ { assert(0); return; } //ok
",
                new FunctionDefinition(
                    new WeakFunctionSignature(
                        FunctionModifier.Harness,
                        new("void"),
                        new("main"),
                        Array.Empty<IVariableInfo>()
                    ),
                    new AssertStatement(Lit(0)),
                    new ReturnStatement()
                )
            },

            new object[]{@"
harness void main (int Out_0_s0_v0, int Out_0_s0_v1) {
    // Assemble structs
    Out_0 Out_0_s0 = new Out_0(v0 = Out_0_s0_v0);
    return;
}
",
                new FunctionDefinition(

                    new WeakFunctionSignature(
                        FunctionModifier.Harness,
                        new("void"),
                        new("main"),
                        new[]{VDec("int","Out_0_s0_v0"), VDec("int", "Out_0_s0_v1") }
                    ),
                    VDec("Out_0","Out_0_s0",new StructNew(new("Out_0"),new[]{Var("v0").Assign(Var("Out_0_s0_v0")) })),
                    new ReturnStatement()
                )
            }
        };

        [DataTestMethod]
        [DynamicData(nameof(FunctionDefinitionCases))]
        public void TestParseFunctionDefinition(string str, object value) {
            var a = SketchParser.FunctionDefinition.Parse(str);
            if (!a.Equals(value)) {

                var x = 5;
            }
            Assert.AreEqual(value, a);
        }




//        /***** SelectedFunctions ******/
//        static IEnumerable<object[]> SelectedFunctionsCases => new[] {
//            new object[] { @"

//harness void main (int Out_0_s0_v0, int Out_0_s0_v1) {
//    // Assemble structs
//    Out_0 Out_0_s0 = new Out_0(v0 = Out_0_s0_v0);
//    return;
//}
//int the_target () {
//    return 1;
//}
//int other() {
//    return 1;
//}


//",
//                new FunctionDefinition(new WeakFunctionSignature(FunctionModifier.None,IntType.Id,new("the_target"),Array.Empty<IVariableInfo>()),X.Return(X.L1))
//            }
//        };

//        [DataTestMethod]
//        [DynamicData(nameof(SelectedFunctionsCases))]
//        public void TestParseSelectedFunctions(string str, object value) {
//            var expected = (FunctionDefinition)value;
//            Assert.AreEqual(expected, SketchParser.AnyFunctionIn(new Identifier[] { expected.Id }).Parse(str));
//        }

        //[DataTestMethod]
        //[DataRow("pattern")]
        //public void TestParseSelectedFunctionsFails(string str) {
        //    AssertEmpty(SketchParser.SelectedFunctions.Many().Parse(str));
        //}






        static WeakVariableDeclaration VDec(string t, string n) => new(new(t), new(n));
        static WeakVariableDeclaration VDec(string t, string n, IExpression def) => new(new(t), new(n), def);

        static Literal Lit(int i) => new(i);

        static VariableRef Var(string s) => new(new(s));

        static IExpression GenOperand(Random r) {
            var k = r.NextSingle();
            if (k > 0.7) return Var("x");
            if (k > 0.4) return new Literal(10);
            if (k > 0.2) return Var("x").Get(new("y"));
            if (k > 0.05) return new FunctionEval(new("f"));

            return new Ternary(new Hole(), new Literal(1), new Literal(2));
        }

        static IExpression GenInfix(int seed, int n) {
            var rand = new Random(seed);
            var ops = Enum.GetValues<Op>();

            var head = GenOperand(rand);

            return InfixOperation.GroupOperators(head, Enumerable.Range(0, n).Select(i =>
                (ops[rand.Next(ops.Length)], GenOperand(rand))
            ));
        }
        static object[] Exemplify(IExpression ex) => new object[] { ex.ToString()!, ex };

        static object[] Exemplify(IStatement st) => new object[] { st.PrettyPrint(), st };

    }
}