using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTEJA_SemestralniPrace.AST
{
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    // Program
    public class Program : ASTNode
    {
        public ProcedureDeclaration MainProcedure { get; set; }
    }

    // Deklarace procedury/funkce
    public abstract class SubprogramDeclaration : Declaration
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public List<Declaration> Declarations { get; set; } = new List<Declaration>();
        public List<Statement> Statements { get; set; } = new List<Statement>();
    }

    public class ProcedureDeclaration : SubprogramDeclaration { }

    public class FunctionDeclaration : SubprogramDeclaration
    {
        public TypeName ReturnType { get; set; }
    }

    // Parametr
    public class Parameter : ASTNode
    {
        public List<string> Names { get; set; } = new List<string>();
        public ParameterMode Mode { get; set; } = ParameterMode.In;
        public TypeName Type { get; set; }
    }

    public enum ParameterMode
    {
        In,
        Out,
        InOut
    }

    // Deklarace
    public abstract class Declaration : ASTNode { }

    public class VariableDeclaration : Declaration
    {
        public List<string> Names { get; set; } = new List<string>();
        public TypeName Type { get; set; }
        public Expression Initializer { get; set; }
    }

    public class TypeDeclaration : Declaration
    {
        public string Name { get; set; }
        public TypeDefinition Definition { get; set; }
    }

    // Definice typu
    public abstract class TypeDefinition : ASTNode { }

    public class ArrayTypeDefinition : TypeDefinition
    {
        public List<IndexRange> IndexRanges { get; set; } = new List<IndexRange>();
        public TypeName ElementType { get; set; }
    }

    public class IndexRange : ASTNode
    {
        public Expression Lower { get; set; }
        public Expression Upper { get; set; }
    }

    // Typ
    public class TypeName : ASTNode
    {
        public string Name { get; set; }

        public bool IsBuiltIn => Name == "Integer" || Name == "Real" || Name == "String";
    }

    // Příkazy
    public abstract class Statement : ASTNode { }

    public class AssignmentStatement : Statement
    {
        public Variable Target { get; set; }
        public Expression Value { get; set; }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public List<Statement> ThenStatements { get; set; } = new List<Statement>();
        public List<ElsifClause> ElsifClauses { get; set; } = new List<ElsifClause>();
        public List<Statement> ElseStatements { get; set; } = new List<Statement>();
    }

    public class ElsifClause : ASTNode
    {
        public Expression Condition { get; set; }
        public List<Statement> Statements { get; set; } = new List<Statement>();
    }

    public class LoopStatement : Statement
    {
        public string Label { get; set; }
        public List<Statement> Statements { get; set; } = new List<Statement>();
    }

    public class ForStatement : Statement
    {
        public string Iterator { get; set; }
        public bool Reverse { get; set; }
        public Expression LowerBound { get; set; }
        public Expression UpperBound { get; set; }
        public List<Statement> Statements { get; set; } = new List<Statement>();
    }

    public class ProcedureCallStatement : Statement
    {
        public string Name { get; set; }
        public List<Expression> Arguments { get; set; } = new List<Expression>();
    }

    public class ReturnStatement : Statement
    {
        public Expression Value { get; set; }
    }

    public class ExitStatement : Statement
    {
        public string Label { get; set; }
    }

    // Výrazy
    public abstract class Expression : ASTNode
    {
        public TypeName ExpressionType { get; set; }
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public BinaryOperator Operator { get; set; }
        public Expression Right { get; set; }
    }

    public enum BinaryOperator
    {
        Add, Subtract, Multiply, Divide, Mod, Power,
        And, Or,
        Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual
    }

    public class UnaryExpression : Expression
    {
        public UnaryOperator Operator { get; set; }
        public Expression Operand { get; set; }
    }

    public enum UnaryOperator
    {
        Plus, Minus, Not
    }

    public class IntegerLiteral : Expression
    {
        public int Value { get; set; }
    }

    public class RealLiteral : Expression
    {
        public double Value { get; set; }
    }

    public class StringLiteral : Expression
    {
        public string Value { get; set; }
    }

    public class Variable : Expression
    {
        public string Name { get; set; }
        public List<Expression> ArrayIndices { get; set; }
    }

    public class FunctionCall : Expression
    {
        public string Name { get; set; }
        public List<Expression> Arguments { get; set; } = new List<Expression>();
    }
}
