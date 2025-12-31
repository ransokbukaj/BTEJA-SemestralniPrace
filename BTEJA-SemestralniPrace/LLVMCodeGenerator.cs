using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp.Interop;
using BTEJA_SemestralniPrace.AST;

namespace BTEJA_SemestralniPrace
{
    public class LLVMCodeGenerator
    {
        private LLVMModuleRef module;
        private LLVMBuilderRef builder;
        private Dictionary<string, LLVMValueRef> namedValues;
        private Dictionary<string, LLVMTypeRef> namedTypes;
        private LLVMValueRef currentFunction;
        private Stack<Dictionary<string, LLVMValueRef>> scopeStack;

        public LLVMCodeGenerator(string moduleName)
        {
            module = LLVMModuleRef.CreateWithName(moduleName);
            builder = module.Context.CreateBuilder();
            namedValues = new Dictionary<string, LLVMValueRef>();
            namedTypes = new Dictionary<string, LLVMTypeRef>();
            scopeStack = new Stack<Dictionary<string, LLVMValueRef>>();

            InitializeBuiltinTypes();
            DeclareBuiltinFunctions();
        }

        private void InitializeBuiltinTypes()
        {
            namedTypes["Integer"] = LLVMTypeRef.Int32;
            namedTypes["Real"] = LLVMTypeRef.Double;
            namedTypes["String"] = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        }

        private void DeclareBuiltinFunctions()
        {
            // printf pro výstup
            var printfType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
                true
            );
            module.AddFunction("printf", printfType);

            // scanf pro vstup
            var scanfType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
                true
            );
            module.AddFunction("scanf", scanfType);

            // Matematické funkce
            var sqrtType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, new[] { LLVMTypeRef.Double });
            module.AddFunction("sqrt", sqrtType);

            var sinType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, new[] { LLVMTypeRef.Double });
            module.AddFunction("sin", sinType);

            var cosType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, new[] { LLVMTypeRef.Double });
            module.AddFunction("cos", cosType);
        }

        public void Generate(AST.Program program)
        {
            GenerateSubprogram(program.MainProcedure);
        }

        private void GenerateSubprogram(SubprogramDeclaration subprogram)
        {
            EnterScope();

            // Určit návratový typ
            LLVMTypeRef returnType;
            if (subprogram is FunctionDeclaration func)
            {
                returnType = GetLLVMType(func.ReturnType);
            }
            else
            {
                returnType = LLVMTypeRef.Void;
            }

            // Vytvořit typy parametrů
            var paramTypes = subprogram.Parameters
                .SelectMany(p => p.Names.Select(n => GetLLVMType(p.Type)))
                .ToArray();

            // Vytvořit typ funkce
            var functionType = LLVMTypeRef.CreateFunction(returnType, paramTypes);

            // Vytvořit funkci
            var function = module.AddFunction(subprogram.Name, functionType);
            currentFunction = function;

            // Vytvořit základní blok
            var entryBlock = function.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBlock);

            // Alokovat parametry
            int paramIndex = 0;
            foreach (var param in subprogram.Parameters)
            {
                foreach (var name in param.Names)
                {
                    var paramValue = function.GetParam((uint)paramIndex);
                    paramValue.Name = name;

                    // Alokovat prostor pro parametr a uložit hodnotu
                    var alloca = builder.BuildAlloca(GetLLVMType(param.Type), name);
                    builder.BuildStore(paramValue, alloca);
                    namedValues[name] = alloca;

                    paramIndex++;
                }
            }

            // Zpracovat deklarace
            foreach (var decl in subprogram.Declarations)
            {
                GenerateDeclaration(decl);
            }

            // Zpracovat příkazy
            foreach (var stmt in subprogram.Statements)
            {
                GenerateStatement(stmt);
            }

            // Pokud funkce nemá explicitní return, přidat ho
            if (returnType.Kind == LLVMTypeKind.LLVMVoidTypeKind)
            {
                if (!builder.InsertBlock.Terminator.Handle.Equals(IntPtr.Zero))
                {
                    builder.BuildRetVoid();
                }
            }

            ExitScope();
        }

        private void GenerateDeclaration(Declaration decl)
        {
            switch (decl)
            {
                case VariableDeclaration varDecl:
                    foreach (var name in varDecl.Names)
                    {
                        var type = GetLLVMType(varDecl.Type);
                        var alloca = builder.BuildAlloca(type, name);
                        namedValues[name] = alloca;

                        if (varDecl.Initializer != null)
                        {
                            var initValue = GenerateExpression(varDecl.Initializer);
                            builder.BuildStore(initValue, alloca);
                        }
                    }
                    break;

                case TypeDeclaration typeDecl:
                    GenerateTypeDefinition(typeDecl.Name, typeDecl.Definition);
                    break;

                case SubprogramDeclaration subDecl:
                    GenerateSubprogram(subDecl);
                    break;
            }
        }

        private void GenerateTypeDefinition(string name, TypeDefinition definition)
        {
            if (definition is ArrayTypeDefinition arrayDef)
            {
                var elementType = GetLLVMType(arrayDef.ElementType);

                // Vypočítat celkovou velikost pole
                uint totalSize = 1;
                foreach (var range in arrayDef.IndexRanges)
                {
                    // Zjednodušení: předpokládáme konstantní rozsahy
                    if (range.Lower is IntegerLiteral lower && range.Upper is IntegerLiteral upper)
                    {
                        totalSize *= (uint)(upper.Value - lower.Value + 1);
                    }
                }

                var arrayType = LLVMTypeRef.CreateArray(elementType, totalSize);
                namedTypes[name] = arrayType;
            }
        }

        private void GenerateStatement(Statement stmt)
        {
            switch (stmt)
            {
                case AssignmentStatement assign:
                    var target = GetVariablePointer(assign.Target);
                    var value = GenerateExpression(assign.Value);
                    builder.BuildStore(value, target);
                    break;

                case IfStatement ifStmt:
                    GenerateIfStatement(ifStmt);
                    break;

                case LoopStatement loop:
                    GenerateLoopStatement(loop);
                    break;

                case ForStatement forStmt:
                    GenerateForStatement(forStmt);
                    break;

                case ProcedureCallStatement call:
                    GenerateProcedureCall(call);
                    break;

                case ReturnStatement ret:
                    if (ret.Value != null)
                    {
                        var retValue = GenerateExpression(ret.Value);
                        builder.BuildRet(retValue);
                    }
                    else
                    {
                        builder.BuildRetVoid();
                    }
                    break;

                case ExitStatement exit:
                    // Implementace exit pro loop
                    // TODO: potřebujeme sledovat aktuální loop context
                    break;
            }
        }

        private void GenerateIfStatement(IfStatement ifStmt)
        {
            var condition = GenerateExpression(ifStmt.Condition);
            var condBool = builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condition,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false), "ifcond");

            var thenBlock = currentFunction.AppendBasicBlock("then");
            var elseBlock = currentFunction.AppendBasicBlock("else");
            var mergeBlock = currentFunction.AppendBasicBlock("ifcont");

            builder.BuildCondBr(condBool, thenBlock, elseBlock);

            // Then blok
            builder.PositionAtEnd(thenBlock);
            foreach (var stmt in ifStmt.ThenStatements)
            {
                GenerateStatement(stmt);
            }
            if (builder.InsertBlock.Terminator.Handle.Equals(IntPtr.Zero))
            {
                builder.BuildBr(mergeBlock);
            }

            // Else blok (včetně elsif)
            builder.PositionAtEnd(elseBlock);
            if (ifStmt.ElsifClauses.Count > 0 || ifStmt.ElseStatements.Count > 0)
            {
                // TODO: Implementovat elsif
                foreach (var stmt in ifStmt.ElseStatements)
                {
                    GenerateStatement(stmt);
                }
            }
            if (builder.InsertBlock.Terminator.Handle.Equals(IntPtr.Zero))
            {
                builder.BuildBr(mergeBlock);
            }

            builder.PositionAtEnd(mergeBlock);
        }

        private void GenerateLoopStatement(LoopStatement loop)
        {
            var loopBlock = currentFunction.AppendBasicBlock("loop");
            var afterBlock = currentFunction.AppendBasicBlock("afterloop");

            builder.BuildBr(loopBlock);
            builder.PositionAtEnd(loopBlock);

            foreach (var stmt in loop.Statements)
            {
                GenerateStatement(stmt);
            }

            builder.BuildBr(loopBlock);
            builder.PositionAtEnd(afterBlock);
        }

        private void GenerateForStatement(ForStatement forStmt)
        {
            // Alokovat iterátor
            var iteratorAlloca = builder.BuildAlloca(LLVMTypeRef.Int32, forStmt.Iterator);
            namedValues[forStmt.Iterator] = iteratorAlloca;

            // Inicializovat iterátor
            var startValue = GenerateExpression(forStmt.LowerBound);
            builder.BuildStore(startValue, iteratorAlloca);

            var loopBlock = currentFunction.AppendBasicBlock("forloop");
            var afterBlock = currentFunction.AppendBasicBlock("afterfor");

            builder.BuildBr(loopBlock);
            builder.PositionAtEnd(loopBlock);

            // Tělo cyklu
            foreach (var stmt in forStmt.Statements)
            {
                GenerateStatement(stmt);
            }

            // Inkrementace/dekrementace
            var currentValue = builder.BuildLoad2(LLVMTypeRef.Int32, iteratorAlloca, "current");
            var nextValue = forStmt.Reverse
                ? builder.BuildSub(currentValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false), "next")
                : builder.BuildAdd(currentValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false), "next");
            builder.BuildStore(nextValue, iteratorAlloca);

            // Podmínka pokračování
            var endValue = GenerateExpression(forStmt.UpperBound);
            var condition = forStmt.Reverse
                ? builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, nextValue, endValue, "loopcond")
                : builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, nextValue, endValue, "loopcond");

            builder.BuildCondBr(condition, loopBlock, afterBlock);
            builder.PositionAtEnd(afterBlock);
        }

        private void GenerateProcedureCall(ProcedureCallStatement call)
        {
            var function = module.GetNamedFunction(call.Name);
            if (function.Handle == IntPtr.Zero)
            {
                throw new Exception($"Unknown function: {call.Name}");
            }

            var args = call.Arguments.Select(GenerateExpression).ToArray();

            // Získat typ funkce z LLVM typu
            var functionType = function.TypeOf.ElementType;
            builder.BuildCall2(functionType, function, args, "");
        }

        private LLVMValueRef GenerateExpression(Expression expr)
        {
            switch (expr)
            {
                case IntegerLiteral intLit:
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)intLit.Value, true);

                case RealLiteral realLit:
                    return LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, realLit.Value);

                case StringLiteral strLit:
                    return builder.BuildGlobalStringPtr(strLit.Value, "str");

                case Variable variable:
                    var ptr = GetVariablePointer(variable);
                    var varType = GetVariableType(variable);
                    return builder.BuildLoad2(varType, ptr, variable.Name);

                case BinaryExpression binary:
                    return GenerateBinaryExpression(binary);

                case UnaryExpression unary:
                    return GenerateUnaryExpression(unary);

                case FunctionCall funcCall:
                    var func = module.GetNamedFunction(funcCall.Name);
                    if (func.Handle == IntPtr.Zero)
                    {
                        throw new Exception($"Unknown function: {funcCall.Name}");
                    }

                    var funcArgs = funcCall.Arguments.Select(GenerateExpression).ToArray();

                    // Získat typ funkce z LLVM typu
                    var funcType = func.TypeOf.ElementType;
                    return builder.BuildCall2(funcType, func, funcArgs, "calltmp");

                default:
                    throw new NotImplementedException($"Expression type {expr.GetType()} not implemented");
            }
        }

        private LLVMValueRef GenerateBinaryExpression(BinaryExpression binary)
        {
            var left = GenerateExpression(binary.Left);
            var right = GenerateExpression(binary.Right);

            switch (binary.Operator)
            {
                case BinaryOperator.Add:
                    return left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind || right.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                        ? builder.BuildFAdd(left, right, "addtmp")
                        : builder.BuildAdd(left, right, "addtmp");

                case BinaryOperator.Subtract:
                    return left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind || right.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                        ? builder.BuildFSub(left, right, "subtmp")
                        : builder.BuildSub(left, right, "subtmp");

                case BinaryOperator.Multiply:
                    return left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind || right.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                        ? builder.BuildFMul(left, right, "multmp")
                        : builder.BuildMul(left, right, "multmp");

                case BinaryOperator.Divide:
                    return left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind || right.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                        ? builder.BuildFDiv(left, right, "divtmp")
                        : builder.BuildSDiv(left, right, "divtmp");

                case BinaryOperator.Equal:
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eqtmp");

                case BinaryOperator.NotEqual:
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "netmp");

                case BinaryOperator.Less:
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lttmp");

                case BinaryOperator.LessOrEqual:
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "letmp");

                case BinaryOperator.Greater:
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gttmp");

                case BinaryOperator.GreaterOrEqual:
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "getmp");

                case BinaryOperator.And:
                    return builder.BuildAnd(left, right, "andtmp");

                case BinaryOperator.Or:
                    return builder.BuildOr(left, right, "ortmp");

                default:
                    throw new NotImplementedException($"Binary operator {binary.Operator} not implemented");
            }
        }

        private LLVMValueRef GenerateUnaryExpression(UnaryExpression unary)
        {
            var operand = GenerateExpression(unary.Operand);

            switch (unary.Operator)
            {
                case UnaryOperator.Minus:
                    return operand.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                        ? builder.BuildFNeg(operand, "negtmp")
                        : builder.BuildNeg(operand, "negtmp");

                case UnaryOperator.Plus:
                    return operand;

                case UnaryOperator.Not:
                    return builder.BuildNot(operand, "nottmp");

                default:
                    throw new NotImplementedException($"Unary operator {unary.Operator} not implemented");
            }
        }

        private LLVMValueRef GetVariablePointer(Variable variable)
        {
            if (!namedValues.TryGetValue(variable.Name, out var value))
            {
                throw new Exception($"Unknown variable: {variable.Name}");
            }

            if (variable.ArrayIndices != null && variable.ArrayIndices.Count > 0)
            {
                // Přístup k poli
                var indices = new List<LLVMValueRef> { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false) };
                indices.AddRange(variable.ArrayIndices.Select(GenerateExpression));
                return builder.BuildGEP2(value.TypeOf, value, indices.ToArray(), "arrayidx");
            }

            return value;
        }

        private LLVMTypeRef GetVariableType(Variable variable)
        {
            var ptr = namedValues[variable.Name];
            return ptr.TypeOf.ElementType;
        }

        private LLVMTypeRef GetLLVMType(TypeName typeName)
        {
            if (namedTypes.TryGetValue(typeName.Name, out var type))
            {
                return type;
            }

            throw new Exception($"Unknown type: {typeName.Name}");
        }

        private void EnterScope()
        {
            scopeStack.Push(new Dictionary<string, LLVMValueRef>(namedValues));
        }

        private void ExitScope()
        {
            namedValues = scopeStack.Pop();
        }

        public void WriteToFile(string filename)
        {
            module.PrintToFile(filename);
        }

        public void WriteBitcodeToFile(string filename)
        {
            module.WriteBitcodeToFile(filename);
        }

        public string PrintToString()
        {
            return module.PrintToString();
        }
    }
}