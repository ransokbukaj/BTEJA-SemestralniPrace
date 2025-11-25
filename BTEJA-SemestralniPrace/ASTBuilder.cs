using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using BTEJA_SemestralniPrace.AST;

namespace BTEJA_SemestralniPrace
{
    public class ASTBuilder : AdaBaseVisitor<object>
    {
        public override object VisitProgram([NotNull] AdaParser.ProgramContext context)
        {
            var program = new AST.Program();
            program.MainProcedure = (ProcedureDeclaration)Visit(context.procedureDeclaration());
            return program;
        }

        public override object VisitProcedureDeclaration([NotNull] AdaParser.ProcedureDeclarationContext context)
        {
            if (context.GetChild(0).GetText() == "procedure")
            {
                var proc = new ProcedureDeclaration
                {
                    Name = context.identifier(0).GetText(),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };

                if (context.formalParameters() != null)
                {
                    proc.Parameters = (List<Parameter>)Visit(context.formalParameters());
                }

                proc.Declarations = (List<Declaration>)Visit(context.declarations());
                proc.Statements = (List<Statement>)Visit(context.statements());

                return proc;
            }
            else
            {
                var func = new FunctionDeclaration
                {
                    Name = context.identifier(0).GetText(),
                    ReturnType = (TypeName)Visit(context.typeName()),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };

                if (context.formalParameters() != null)
                {
                    func.Parameters = (List<Parameter>)Visit(context.formalParameters());
                }

                func.Declarations = (List<Declaration>)Visit(context.declarations());
                func.Statements = (List<Statement>)Visit(context.statements());

                return func;
            }
        }

        public override object VisitFormalParameters([NotNull] AdaParser.FormalParametersContext context)
        {
            return Visit(context.parameterList());
        }

        public override object VisitParameterList([NotNull] AdaParser.ParameterListContext context)
        {
            var parameters = new List<Parameter>();
            foreach (var paramCtx in context.parameter())
            {
                parameters.Add((Parameter)Visit(paramCtx));
            }
            return parameters;
        }

        public override object VisitParameter([NotNull] AdaParser.ParameterContext context)
        {
            var param = new Parameter
            {
                Names = (List<string>)Visit(context.identifierList()),
                Type = (TypeName)Visit(context.typeName()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.parameterMode() != null)
            {
                var modeText = context.parameterMode().GetText();
                param.Mode = modeText switch
                {
                    "in" => ParameterMode.In,
                    "out" => ParameterMode.Out,
                    "inout" => ParameterMode.InOut,
                    _ => ParameterMode.In
                };
            }

            return param;
        }

        public override object VisitIdentifierList([NotNull] AdaParser.IdentifierListContext context)
        {
            return context.identifier().Select(id => id.GetText()).ToList();
        }

        public override object VisitDeclarations([NotNull] AdaParser.DeclarationsContext context)
        {
            var declarations = new List<Declaration>();
            foreach (var declCtx in context.declaration())
            {
                declarations.Add((Declaration)Visit(declCtx));
            }
            return declarations;
        }

        public override object VisitDeclaration([NotNull] AdaParser.DeclarationContext context)
        {
            if (context.variableDeclaration() != null)
                return Visit(context.variableDeclaration());
            if (context.typeDeclaration() != null)
                return Visit(context.typeDeclaration());
            return Visit(context.procedureDeclaration());
        }

        public override object VisitVariableDeclaration([NotNull] AdaParser.VariableDeclarationContext context)
        {
            var varDecl = new VariableDeclaration
            {
                Names = (List<string>)Visit(context.identifierList()),
                Type = (TypeName)Visit(context.typeName()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.expression() != null)
            {
                varDecl.Initializer = (Expression)Visit(context.expression());
            }

            return varDecl;
        }

        public override object VisitTypeDeclaration([NotNull] AdaParser.TypeDeclarationContext context)
        {
            return new TypeDeclaration
            {
                Name = context.identifier().GetText(),
                Definition = (TypeDefinition)Visit(context.typeDefinition()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitTypeDefinition([NotNull] AdaParser.TypeDefinitionContext context)
        {
            var arrayType = new ArrayTypeDefinition
            {
                ElementType = (TypeName)Visit(context.typeName()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            foreach (var rangeCtx in context.indexRange())
            {
                arrayType.IndexRanges.Add((IndexRange)Visit(rangeCtx));
            }

            return arrayType;
        }

        public override object VisitIndexRange([NotNull] AdaParser.IndexRangeContext context)
        {
            return new IndexRange
            {
                Lower = (Expression)Visit(context.expression(0)),
                Upper = (Expression)Visit(context.expression(1)),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitTypeName([NotNull] AdaParser.TypeNameContext context)
        {
            return new TypeName
            {
                Name = context.GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitStatements([NotNull] AdaParser.StatementsContext context)
        {
            var statements = new List<Statement>();
            foreach (var stmtCtx in context.statement())
            {
                statements.Add((Statement)Visit(stmtCtx));
            }
            return statements;
        }

        public override object VisitStatement([NotNull] AdaParser.StatementContext context)
        {
            if (context.assignmentStatement() != null)
                return Visit(context.assignmentStatement());
            if (context.ifStatement() != null)
                return Visit(context.ifStatement());
            if (context.loopStatement() != null)
                return Visit(context.loopStatement());
            if (context.forStatement() != null)
                return Visit(context.forStatement());
            if (context.procedureCall() != null)
                return Visit(context.procedureCall());
            if (context.exitStatement() != null)
                return Visit(context.exitStatement());
            return Visit(context.returnStatement());
        }

        public override object VisitAssignmentStatement([NotNull] AdaParser.AssignmentStatementContext context)
        {
            return new AssignmentStatement
            {
                Target = (Variable)Visit(context.variable()),
                Value = (Expression)Visit(context.expression()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitIfStatement([NotNull] AdaParser.IfStatementContext context)
        {
            var ifStmt = new IfStatement
            {
                Condition = (Expression)Visit(context.expression(0)),
                ThenStatements = (List<Statement>)Visit(context.statements(0)),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            int exprIndex = 1;
            int stmtIndex = 1;

            for (int i = 0; i < context.ChildCount; i++)
            {
                if (context.GetChild(i).GetText() == "elsif")
                {
                    var elsif = new ElsifClause
                    {
                        Condition = (Expression)Visit(context.expression(exprIndex++)),
                        Statements = (List<Statement>)Visit(context.statements(stmtIndex++))
                    };
                    ifStmt.ElsifClauses.Add(elsif);
                }
                else if (context.GetChild(i).GetText() == "else")
                {
                    ifStmt.ElseStatements = (List<Statement>)Visit(context.statements(stmtIndex++));
                    break;
                }
            }

            return ifStmt;
        }

        public override object VisitLoopStatement([NotNull] AdaParser.LoopStatementContext context)
        {
            var loop = new LoopStatement
            {
                Statements = (List<Statement>)Visit(context.statements()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.identifier().Length > 0)
            {
                loop.Label = context.identifier(0).GetText();
            }

            return loop;
        }

        public override object VisitForStatement([NotNull] AdaParser.ForStatementContext context)
        {
            return new ForStatement
            {
                Iterator = context.identifier().GetText(),
                Reverse = context.GetText().Contains("reverse"),
                LowerBound = (Expression)Visit(context.expression(0)),
                UpperBound = (Expression)Visit(context.expression(1)),
                Statements = (List<Statement>)Visit(context.statements()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitProcedureCall([NotNull] AdaParser.ProcedureCallContext context)
        {
            var call = new ProcedureCallStatement
            {
                Name = context.identifier().GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.actualParameters() != null)
            {
                call.Arguments = (List<Expression>)Visit(context.actualParameters());
            }

            return call;
        }

        public override object VisitActualParameters([NotNull] AdaParser.ActualParametersContext context)
        {
            return Visit(context.expressionList());
        }

        public override object VisitExpressionList([NotNull] AdaParser.ExpressionListContext context)
        {
            return context.expression().Select(e => (Expression)Visit(e)).ToList();
        }

        public override object VisitReturnStatement([NotNull] AdaParser.ReturnStatementContext context)
        {
            var ret = new ReturnStatement
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.expression() != null)
            {
                ret.Value = (Expression)Visit(context.expression());
            }

            return ret;
        }

        public override object VisitExitStatement([NotNull] AdaParser.ExitStatementContext context)
        {
            var exit = new ExitStatement
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.identifier() != null)
            {
                exit.Label = context.identifier().GetText();
            }

            return exit;
        }

        public override object VisitExpression([NotNull] AdaParser.ExpressionContext context)
        {
            if (context.relationalOperator() != null)
            {
                return new BinaryExpression
                {
                    Left = (Expression)Visit(context.simpleExpression(0)),
                    Operator = GetRelationalOperator(context.relationalOperator().GetText()),
                    Right = (Expression)Visit(context.simpleExpression(1)),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }

            return Visit(context.simpleExpression(0));
        }

        public override object VisitSimpleExpression([NotNull] AdaParser.SimpleExpressionContext context)
        {
            Expression expr = (Expression)Visit(context.term(0));

            if (context.sign() != null)
            {
                expr = new UnaryExpression
                {
                    Operator = context.sign().GetText() == "+" ? UnaryOperator.Plus : UnaryOperator.Minus,
                    Operand = expr,
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }

            for (int i = 0; i < context.addingOperator().Length; i++)
            {
                expr = new BinaryExpression
                {
                    Left = expr,
                    Operator = GetAddingOperator(context.addingOperator(i).GetText()),
                    Right = (Expression)Visit(context.term(i + 1)),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }

            return expr;
        }

        public override object VisitTerm([NotNull] AdaParser.TermContext context)
        {
            Expression expr = (Expression)Visit(context.factor(0));

            for (int i = 0; i < context.multiplyingOperator().Length; i++)
            {
                expr = new BinaryExpression
                {
                    Left = expr,
                    Operator = GetMultiplyingOperator(context.multiplyingOperator(i).GetText()),
                    Right = (Expression)Visit(context.factor(i + 1)),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }

            return expr;
        }

        public override object VisitFactor([NotNull] AdaParser.FactorContext context)
        {
            Expression expr = (Expression)Visit(context.primary(0));

            if (context.primary().Length > 1)
            {
                expr = new BinaryExpression
                {
                    Left = expr,
                    Operator = BinaryOperator.Power,
                    Right = (Expression)Visit(context.primary(1)),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }

            return expr;
        }

        public override object VisitPrimary([NotNull] AdaParser.PrimaryContext context)
        {
            if (context.integerLiteral() != null)
                return Visit(context.integerLiteral());
            if (context.realLiteral() != null)
                return Visit(context.realLiteral());
            if (context.stringLiteral() != null)
                return Visit(context.stringLiteral());
            if (context.variable() != null)
                return Visit(context.variable());
            if (context.functionCall() != null)
                return Visit(context.functionCall());
            if (context.expression() != null)
                return Visit(context.expression());

            return new UnaryExpression
            {
                Operator = UnaryOperator.Not,
                Operand = (Expression)Visit(context.primary()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitVariable([NotNull] AdaParser.VariableContext context)
        {
            var variable = new Variable
            {
                Name = context.identifier().GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.arrayAccess() != null)
            {
                variable.ArrayIndices = (List<Expression>)Visit(context.arrayAccess());
            }

            return variable;
        }

        public override object VisitArrayAccess([NotNull] AdaParser.ArrayAccessContext context)
        {
            return context.expression().Select(e => (Expression)Visit(e)).ToList();
        }

        public override object VisitFunctionCall([NotNull] AdaParser.FunctionCallContext context)
        {
            return new FunctionCall
            {
                Name = context.identifier().GetText(),
                Arguments = (List<Expression>)Visit(context.actualParameters()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitIntegerLiteral([NotNull] AdaParser.IntegerLiteralContext context)
        {
            return new IntegerLiteral
            {
                Value = int.Parse(context.GetText()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitRealLiteral([NotNull] AdaParser.RealLiteralContext context)
        {
            return new RealLiteral
            {
                Value = double.Parse(context.GetText()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        public override object VisitStringLiteral([NotNull] AdaParser.StringLiteralContext context)
        {
            var text = context.GetText();
            return new StringLiteral
            {
                Value = text.Substring(1, text.Length - 2),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        private BinaryOperator GetRelationalOperator(string op)
        {
            return op switch
            {
                "=" => BinaryOperator.Equal,
                "/=" => BinaryOperator.NotEqual,
                "<" => BinaryOperator.Less,
                "<=" => BinaryOperator.LessOrEqual,
                ">" => BinaryOperator.Greater,
                ">=" => BinaryOperator.GreaterOrEqual,
                _ => throw new ArgumentException($"Unknown relational operator: {op}")
            };
        }

        private BinaryOperator GetAddingOperator(string op)
        {
            return op switch
            {
                "+" => BinaryOperator.Add,
                "-" => BinaryOperator.Subtract,
                "or" => BinaryOperator.Or,
                _ => throw new ArgumentException($"Unknown adding operator: {op}")
            };
        }

        private BinaryOperator GetMultiplyingOperator(string op)
        {
            return op switch
            {
                "*" => BinaryOperator.Multiply,
                "/" => BinaryOperator.Divide,
                "mod" => BinaryOperator.Mod,
                "and" => BinaryOperator.And,
                _ => throw new ArgumentException($"Unknown multiplying operator: {op}")
            };
        }
    }
}