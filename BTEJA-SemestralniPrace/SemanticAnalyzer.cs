using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BTEJA_SemestralniPrace.AST;

namespace BTEJA_SemestralniPrace
{
    public class SemanticAnalyzer
    {
        private SymbolTable symbolTable;
        private List<string> errors = new List<string>();
        private SubprogramDeclaration currentSubprogram;
        private NativeFunctionManager nativeFunctions;

        public List<string> Errors => errors;

        public SemanticAnalyzer()
        {
            nativeFunctions = new NativeFunctionManager();
        }

        public void Analyze(AST.Program program)
        {
            symbolTable = new SymbolTable();
            AnalyzeSubprogram(program.MainProcedure);
        }

        private void AnalyzeSubprogram(SubprogramDeclaration subprogram)
        {
            currentSubprogram = subprogram;
            symbolTable.EnterScope();

            // Přidat parametry do tabulky symbolů
            foreach (var param in subprogram.Parameters)
            {
                foreach (var name in param.Names)
                {
                    if (!symbolTable.AddSymbol(name, new VariableSymbol
                    {
                        Name = name,
                        Type = param.Type,
                        IsParameter = true,
                        Mode = param.Mode
                    }))
                    {
                        AddError($"Parameter '{name}' již byl deklarován", param.Line, param.Column);
                    }
                }
            }

            // Analyzovat deklarace
            foreach (var decl in subprogram.Declarations)
            {
                AnalyzeDeclaration(decl);
            }

            // Analyzovat příkazy
            foreach (var stmt in subprogram.Statements)
            {
                AnalyzeStatement(stmt);
            }

            // Kontrola return pro funkce
            if (subprogram is FunctionDeclaration func)
            {
                if (!HasReturnStatement(subprogram.Statements))
                {
                    AddError($"Funkce '{func.Name}' musí obsahovat return příkaz", func.Line, func.Column);
                }
            }

            symbolTable.ExitScope();
        }

        private void AnalyzeDeclaration(Declaration decl)
        {
            switch (decl)
            {
                case VariableDeclaration varDecl:
                    foreach (var name in varDecl.Names)
                    {
                        if (!symbolTable.AddSymbol(name, new VariableSymbol
                        {
                            Name = name,
                            Type = varDecl.Type
                        }))
                        {
                            AddError($"Proměnná '{name}' již byla deklarována", varDecl.Line, varDecl.Column);
                        }
                    }

                    if (varDecl.Initializer != null)
                    {
                        var initType = AnalyzeExpression(varDecl.Initializer);
                        if (!AreTypesCompatible(varDecl.Type, initType))
                        {
                            AddError($"Nekompatibilní typy při inicializaci proměnné", varDecl.Line, varDecl.Column);
                        }
                    }
                    break;

                case TypeDeclaration typeDecl:
                    if (!symbolTable.AddSymbol(typeDecl.Name, new TypeSymbol
                    {
                        Name = typeDecl.Name,
                        Definition = typeDecl.Definition
                    }))
                    {
                        AddError($"Typ '{typeDecl.Name}' již byl deklarován", typeDecl.Line, typeDecl.Column);
                    }
                    break;

                case ProcedureDeclaration procDecl:
                    if (!symbolTable.AddSymbol(procDecl.Name, new SubprogramSymbol
                    {
                        Name = procDecl.Name,
                        Parameters = procDecl.Parameters,
                        IsFunction = false
                    }))
                    {
                        AddError($"Procedura '{procDecl.Name}' již byla deklarována", procDecl.Line, procDecl.Column);
                    }
                    AnalyzeSubprogram(procDecl);
                    break;

                case FunctionDeclaration funcDecl:
                    if (!symbolTable.AddSymbol(funcDecl.Name, new SubprogramSymbol
                    {
                        Name = funcDecl.Name,
                        Parameters = funcDecl.Parameters,
                        ReturnType = funcDecl.ReturnType,
                        IsFunction = true
                    }))
                    {
                        AddError($"Funkce '{funcDecl.Name}' již byla deklarována", funcDecl.Line, funcDecl.Column);
                    }
                    AnalyzeSubprogram(funcDecl);
                    break;
            }
        }

        private void AnalyzeStatement(Statement stmt)
        {
            switch (stmt)
            {
                case AssignmentStatement assign:
                    var targetType = AnalyzeVariable(assign.Target);
                    var valueType = AnalyzeExpression(assign.Value);

                    if (targetType != null && valueType != null && !AreTypesCompatible(targetType, valueType))
                    {
                        AddError($"Nekompatibilní typy při přiřazení", assign.Line, assign.Column);
                    }
                    break;

                case IfStatement ifStmt:
                    var condType = AnalyzeExpression(ifStmt.Condition);

                    foreach (var thenStmt in ifStmt.ThenStatements)
                        AnalyzeStatement(thenStmt);

                    foreach (var elsif in ifStmt.ElsifClauses)
                    {
                        AnalyzeExpression(elsif.Condition);
                        foreach (var s in elsif.Statements)
                            AnalyzeStatement(s);
                    }

                    foreach (var elseStmt in ifStmt.ElseStatements)
                        AnalyzeStatement(elseStmt);
                    break;

                case LoopStatement loop:
                    foreach (var loopStmt in loop.Statements)
                        AnalyzeStatement(loopStmt);
                    break;

                case ForStatement forStmt:
                    symbolTable.EnterScope();

                    // Iterátor je implicitně deklarován
                    symbolTable.AddSymbol(forStmt.Iterator, new VariableSymbol
                    {
                        Name = forStmt.Iterator,
                        Type = new TypeName { Name = "Integer" }
                    });

                    AnalyzeExpression(forStmt.LowerBound);
                    AnalyzeExpression(forStmt.UpperBound);

                    foreach (var s in forStmt.Statements)
                        AnalyzeStatement(s);

                    symbolTable.ExitScope();
                    break;

                case ProcedureCallStatement call:
                    // Kontrola nativních funkcí
                    if (nativeFunctions.IsNativeFunction(call.Name))
                    {
                        var nativeInfo = nativeFunctions.GetFunctionInfo(call.Name);
                        if (nativeInfo.ReturnType != null)
                        {
                            AddError($"'{call.Name}' je funkce, ne procedura. Musí být použita ve výrazu.", call.Line, call.Column);
                        }
                        else
                        {
                            CheckNativeArguments(call.Name, nativeInfo, call.Arguments, call.Line, call.Column);
                        }
                        break;
                    }

                    var symbol = symbolTable.LookupSymbol(call.Name);
                    if (symbol == null)
                    {
                        AddError($"Nedefinovaná procedura nebo funkce '{call.Name}'", call.Line, call.Column);
                    }
                    else if (symbol is SubprogramSymbol subSym)
                    {
                        CheckArguments(call.Name, subSym.Parameters, call.Arguments, call.Line, call.Column);
                    }
                    else
                    {
                        AddError($"'{call.Name}' není procedura nebo funkce", call.Line, call.Column);
                    }
                    break;

                case ReturnStatement ret:
                    if (currentSubprogram is FunctionDeclaration func)
                    {
                        if (ret.Value == null)
                        {
                            AddError("Funkce musí vracet hodnotu", ret.Line, ret.Column);
                        }
                        else
                        {
                            var retType = AnalyzeExpression(ret.Value);
                            if (!AreTypesCompatible(func.ReturnType, retType))
                            {
                                AddError($"Návratová hodnota neodpovídá deklarovanému typu funkce", ret.Line, ret.Column);
                            }
                        }
                    }
                    else if (ret.Value != null)
                    {
                        AddError("Procedura nemůže vracet hodnotu", ret.Line, ret.Column);
                    }
                    break;
            }
        }

        private TypeName AnalyzeExpression(Expression expr)
        {
            switch (expr)
            {
                case IntegerLiteral:
                    expr.ExpressionType = new TypeName { Name = "Integer" };
                    return expr.ExpressionType;

                case RealLiteral:
                    expr.ExpressionType = new TypeName { Name = "Real" };
                    return expr.ExpressionType;

                case StringLiteral:
                    expr.ExpressionType = new TypeName { Name = "String" };
                    return expr.ExpressionType;

                case Variable variable:
                    expr.ExpressionType = AnalyzeVariable(variable);
                    return expr.ExpressionType;

                case BinaryExpression binary:
                    var leftType = AnalyzeExpression(binary.Left);
                    var rightType = AnalyzeExpression(binary.Right);

                    if (leftType != null && rightType != null)
                    {
                        if (IsArithmeticOperator(binary.Operator))
                        {
                            if (IsNumericType(leftType) && IsNumericType(rightType))
                            {
                                // Real + Integer = Real
                                if (leftType.Name == "Real" || rightType.Name == "Real")
                                    expr.ExpressionType = new TypeName { Name = "Real" };
                                else
                                    expr.ExpressionType = new TypeName { Name = "Integer" };
                            }
                            else
                            {
                                AddError($"Aritmetické operátory vyžadují numerické typy", binary.Line, binary.Column);
                            }
                        }
                        else if (IsRelationalOperator(binary.Operator))
                        {
                            if (!AreTypesCompatible(leftType, rightType))
                            {
                                AddError($"Operandy relačního operátoru musí mít kompatibilní typy", binary.Line, binary.Column);
                            }
                            expr.ExpressionType = new TypeName { Name = "Integer" }; // Boolean simulován jako Integer
                        }
                        else if (IsLogicalOperator(binary.Operator))
                        {
                            expr.ExpressionType = new TypeName { Name = "Integer" }; // Boolean simulován jako Integer
                        }
                    }
                    return expr.ExpressionType;

                case UnaryExpression unary:
                    var operandType = AnalyzeExpression(unary.Operand);
                    if (operandType != null)
                    {
                        if (unary.Operator == UnaryOperator.Not)
                        {
                            expr.ExpressionType = new TypeName { Name = "Integer" };
                        }
                        else if (IsNumericType(operandType))
                        {
                            expr.ExpressionType = operandType;
                        }
                        else
                        {
                            AddError($"Unární +/- vyžaduje numerický typ", unary.Line, unary.Column);
                        }
                    }
                    return expr.ExpressionType;

                case FunctionCall funcCall:
                    // Kontrola nativních funkcí
                    if (nativeFunctions.IsNativeFunction(funcCall.Name))
                    {
                        var nativeInfo = nativeFunctions.GetFunctionInfo(funcCall.Name);
                        if (nativeInfo.ReturnType == null)
                        {
                            AddError($"'{funcCall.Name}' je procedura, ne funkce. Nemůže být použita ve výrazu.", funcCall.Line, funcCall.Column);
                        }
                        else
                        {
                            CheckNativeArguments(funcCall.Name, nativeInfo, funcCall.Arguments, funcCall.Line, funcCall.Column);
                            expr.ExpressionType = new TypeName { Name = nativeInfo.ReturnType };
                        }
                        return expr.ExpressionType;
                    }

                    var funcSymbol = symbolTable.LookupSymbol(funcCall.Name);
                    if (funcSymbol == null)
                    {
                        AddError($"Nedefinovaná funkce '{funcCall.Name}'", funcCall.Line, funcCall.Column);
                    }
                    else if (funcSymbol is SubprogramSymbol subSym && subSym.IsFunction)
                    {
                        CheckArguments(funcCall.Name, subSym.Parameters, funcCall.Arguments, funcCall.Line, funcCall.Column);
                        expr.ExpressionType = subSym.ReturnType;
                    }
                    else
                    {
                        AddError($"'{funcCall.Name}' není funkce", funcCall.Line, funcCall.Column);
                    }
                    return expr.ExpressionType;

                default:
                    return null;
            }
        }

        private TypeName AnalyzeVariable(Variable variable)
        {
            var symbol = symbolTable.LookupSymbol(variable.Name);

            if (symbol == null)
            {
                AddError($"Nedefinovaná proměnná '{variable.Name}'", variable.Line, variable.Column);
                return null;
            }

            if (symbol is VariableSymbol varSym)
            {
                if (variable.ArrayIndices != null && variable.ArrayIndices.Count > 0)
                {
                    var typeSymbol = symbolTable.LookupSymbol(varSym.Type.Name) as TypeSymbol;
                    if (typeSymbol?.Definition is ArrayTypeDefinition arrayDef)
                    {
                        if (variable.ArrayIndices.Count != arrayDef.IndexRanges.Count)
                        {
                            AddError($"Špatný počet indexů pro pole '{variable.Name}'", variable.Line, variable.Column);
                        }

                        foreach (var index in variable.ArrayIndices)
                        {
                            var indexType = AnalyzeExpression(index);
                            if (indexType != null && indexType.Name != "Integer")
                            {
                                AddError($"Index pole musí být typu Integer", variable.Line, variable.Column);
                            }
                        }

                        return arrayDef.ElementType;
                    }
                    else
                    {
                        AddError($"'{variable.Name}' není pole", variable.Line, variable.Column);
                    }
                }

                return varSym.Type;
            }

            AddError($"'{variable.Name}' není proměnná", variable.Line, variable.Column);
            return null;
        }

        private void CheckArguments(string name, List<Parameter> parameters, List<Expression> arguments, int line, int col)
        {
            if (parameters.Count != arguments.Count)
            {
                AddError($"Špatný počet argumentů pro '{name}'. Očekáváno {parameters.Count}, poskytnutno {arguments.Count}", line, col);
                return;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                var paramType = parameters[i].Type;
                var argType = AnalyzeExpression(arguments[i]);

                if (argType != null && !AreTypesCompatible(paramType, argType))
                {
                    AddError($"Nekompatibilní typ argumentu {i + 1} při volání '{name}'", line, col);
                }
            }
        }

        private bool AreTypesCompatible(TypeName type1, TypeName type2)
        {
            if (type1 == null || type2 == null) return false;

            // Přesná shoda
            if (type1.Name == type2.Name) return true;

            // Integer kompatibilní s Real (implicitní konverze)
            if (type1.Name == "Real" && type2.Name == "Integer") return true;

            return false;
        }

        private bool IsNumericType(TypeName type)
        {
            return type?.Name == "Integer" || type?.Name == "Real";
        }

        private bool IsArithmeticOperator(BinaryOperator op)
        {
            return op == BinaryOperator.Add || op == BinaryOperator.Subtract ||
                   op == BinaryOperator.Multiply || op == BinaryOperator.Divide ||
                   op == BinaryOperator.Mod || op == BinaryOperator.Power;
        }

        private bool IsRelationalOperator(BinaryOperator op)
        {
            return op == BinaryOperator.Equal || op == BinaryOperator.NotEqual ||
                   op == BinaryOperator.Less || op == BinaryOperator.LessOrEqual ||
                   op == BinaryOperator.Greater || op == BinaryOperator.GreaterOrEqual;
        }

        private bool IsLogicalOperator(BinaryOperator op)
        {
            return op == BinaryOperator.And || op == BinaryOperator.Or;
        }

        private bool HasReturnStatement(List<Statement> statements)
        {
            foreach (var stmt in statements)
            {
                if (stmt is ReturnStatement) return true;
                if (stmt is IfStatement ifStmt)
                {
                    if (HasReturnStatement(ifStmt.ThenStatements) ||
                        ifStmt.ElsifClauses.Any(e => HasReturnStatement(e.Statements)) ||
                        HasReturnStatement(ifStmt.ElseStatements))
                        return true;
                }
            }
            return false;
        }

        private void AddError(string message, int line, int column)
        {
            errors.Add($"Řádek {line}, Sloupec {column}: {message}");
        }

        private void CheckNativeArguments(string name, NativeFunctionInfo nativeInfo, List<Expression> arguments, int line, int col)
        {
            if (nativeInfo.ParameterTypes.Length != arguments.Count)
            {
                AddError($"Špatný počet argumentů pro '{name}'. Očekáváno {nativeInfo.ParameterTypes.Length}, poskytnutno {arguments.Count}", line, col);
                return;
            }

            for (int i = 0; i < nativeInfo.ParameterTypes.Length; i++)
            {
                var expectedType = new TypeName { Name = nativeInfo.ParameterTypes[i] };
                var argType = AnalyzeExpression(arguments[i]);

                if (argType != null && !AreTypesCompatible(expectedType, argType))
                {
                    AddError($"Nekompatibilní typ argumentu {i + 1} při volání nativní funkce '{name}'. Očekáváno {expectedType.Name}, poskytnutno {argType.Name}", line, col);
                }
            }
        }
    }

    // Tabulka symbolů
    public class SymbolTable
    {
        private Stack<Dictionary<string, Symbol>> scopes = new Stack<Dictionary<string, Symbol>>();

        public SymbolTable()
        {
            EnterScope();
        }

        public void EnterScope()
        {
            scopes.Push(new Dictionary<string, Symbol>());
        }

        public void ExitScope()
        {
            if (scopes.Count > 0)
                scopes.Pop();
        }

        public bool AddSymbol(string name, Symbol symbol)
        {
            var currentScope = scopes.Peek();
            if (currentScope.ContainsKey(name))
                return false;

            currentScope[name] = symbol;
            return true;
        }

        public Symbol LookupSymbol(string name)
        {
            foreach (var scope in scopes)
            {
                if (scope.TryGetValue(name, out var symbol))
                    return symbol;
            }
            return null;
        }
    }

    // Symboly
    public abstract class Symbol
    {
        public string Name { get; set; }
    }

    public class VariableSymbol : Symbol
    {
        public TypeName Type { get; set; }
        public bool IsParameter { get; set; }
        public ParameterMode Mode { get; set; }
    }

    public class TypeSymbol : Symbol
    {
        public TypeDefinition Definition { get; set; }
    }

    public class SubprogramSymbol : Symbol
    {
        public List<Parameter> Parameters { get; set; }
        public TypeName ReturnType { get; set; }
        public bool IsFunction { get; set; }
    }
}
