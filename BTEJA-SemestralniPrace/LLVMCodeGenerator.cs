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
        private LLVMValueRef currentFunction;

        // Zásobník pro správu scopů proměnných
        private Stack<Dictionary<string, LLVMValueRef>> variableScopes;

        // Globální registry
        private Dictionary<string, LLVMTypeRef> namedTypes;
        private Dictionary<string, FunctionInfo> declaredFunctions;
        private Dictionary<string, ArrayTypeDefinition> arrayDefinitions;

        // Pro správu loop exit pointů
        private Stack<LLVMBasicBlockRef> loopExitBlocks;

        // Error handling
        public List<CompilerError> Errors { get; } = new List<CompilerError>();
        public List<CompilerWarning> Warnings { get; } = new List<CompilerWarning>();

        public LLVMCodeGenerator(string moduleName)
        {
            module = LLVMModuleRef.CreateWithName(moduleName);
            builder = module.Context.CreateBuilder();

            variableScopes = new Stack<Dictionary<string, LLVMValueRef>>();
            namedTypes = new Dictionary<string, LLVMTypeRef>();
            declaredFunctions = new Dictionary<string, FunctionInfo>();
            arrayDefinitions = new Dictionary<string, ArrayTypeDefinition>();
            loopExitBlocks = new Stack<LLVMBasicBlockRef>();

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
            var printfFunc = module.AddFunction("printf", printfType);
            declaredFunctions["printf"] = new FunctionInfo
            {
                Function = printfFunc,
                Type = printfType,
                Name = "printf"
            };

            // sprintf pro konverze
            var sprintfType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] {
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)
                },
                true
            );
            var sprintfFunc = module.AddFunction("sprintf", sprintfType);
            declaredFunctions["sprintf"] = new FunctionInfo
            {
                Function = sprintfFunc,
                Type = sprintfType,
                Name = "sprintf"
            };

            // scanf pro vstup
            var scanfType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
                true
            );
            var scanfFunc = module.AddFunction("scanf", scanfType);
            declaredFunctions["scanf"] = new FunctionInfo
            {
                Function = scanfFunc,
                Type = scanfType,
                Name = "scanf"
            };

            // fgets pro čtení řádku
            var fgetsType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                new[] {
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                    LLVMTypeRef.Int32,
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)  // FILE*
                }
            );
            var fgetsFunc = module.AddFunction("fgets", fgetsType);
            declaredFunctions["fgets"] = new FunctionInfo
            {
                Function = fgetsFunc,
                Type = fgetsType,
                Name = "fgets"
            };

            // stdin globální proměnná
            var stdinType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
            var stdinVar = module.AddGlobal(stdinType, "stdin");
            stdinVar.Linkage = LLVMLinkage.LLVMExternalLinkage;

            // atoi, atof pro konverze ze stringu
            var atoiType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }
            );
            var atoiFunc = module.AddFunction("atoi", atoiType);
            declaredFunctions["atoi"] = new FunctionInfo
            {
                Function = atoiFunc,
                Type = atoiType,
                Name = "atoi"
            };

            var atofType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Double,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }
            );
            var atofFunc = module.AddFunction("atof", atofType);
            declaredFunctions["atof"] = new FunctionInfo
            {
                Function = atofFunc,
                Type = atofType,
                Name = "atof"
            };

            // Funkce pro výstup
            DeclarePutFunction("Put_Line", true);
            DeclarePutFunction("Put", false);
            DeclarePutIntegerFunction();
            DeclarePutRealFunction();
            DeclareNewLineFunction();

            // Funkce pro vstup
            DeclareGetLineFunction();
            DeclareGetIntegerFunction();
            DeclareGetRealFunction();

            // Konverzní funkce
            DeclareConversionFunction("Integer_To_String", new[] { LLVMTypeRef.Int32 }, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
            DeclareConversionFunction("Real_To_String", new[] { LLVMTypeRef.Double }, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
            DeclareConversionFunction("String_To_Integer", new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }, LLVMTypeRef.Int32);
            DeclareConversionFunction("String_To_Real", new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }, LLVMTypeRef.Double);
            DeclareConversionFunction("Integer_To_Real", new[] { LLVMTypeRef.Int32 }, LLVMTypeRef.Double);
            DeclareConversionFunction("Real_To_Integer", new[] { LLVMTypeRef.Double }, LLVMTypeRef.Int32);

            // Matematické funkce
            DeclareSimpleMathFunction("sqrt");
            DeclareSimpleMathFunction("sin");
            DeclareSimpleMathFunction("cos");
            DeclareSimpleMathFunction("tan");
            DeclareSimpleMathFunction("exp");
            DeclareSimpleMathFunction("log");

            // pow má dva parametry
            var powType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Double,
                new[] { LLVMTypeRef.Double, LLVMTypeRef.Double }
            );
            var powFunc = module.AddFunction("pow", powType);
            declaredFunctions["pow"] = new FunctionInfo
            {
                Function = powFunc,
                Type = powType,
                Name = "pow",
                IsBuiltin = true
            };
        }

        private void DeclarePutFunction(string name, bool newline)
        {
            var funcType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Void,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }
            );
            var func = module.AddFunction(name, funcType);
            declaredFunctions[name] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = name,
                IsBuiltin = true,
                NewLine = newline
            };
        }

        private void DeclarePutIntegerFunction()
        {
            var funcType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Void,
                new[] { LLVMTypeRef.Int32 }
            );
            var func = module.AddFunction("Put_Integer", funcType);
            declaredFunctions["Put_Integer"] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = "Put_Integer",
                IsBuiltin = true
            };
        }

        private void DeclarePutRealFunction()
        {
            var funcType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Void,
                new[] { LLVMTypeRef.Double }
            );
            var func = module.AddFunction("Put_Real", funcType);
            declaredFunctions["Put_Real"] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = "Put_Real",
                IsBuiltin = true
            };
        }

        private void DeclareNewLineFunction()
        {
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, Array.Empty<LLVMTypeRef>());
            var func = module.AddFunction("New_Line", funcType);
            declaredFunctions["New_Line"] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = "New_Line",
                IsBuiltin = true
            };
        }

        private void DeclareGetLineFunction()
        {
            // Get_Line(str: out String)
            var funcType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Void,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }
            );
            var func = module.AddFunction("Get_Line", funcType);
            declaredFunctions["Get_Line"] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = "Get_Line",
                IsBuiltin = true
            };
        }

        private void DeclareGetIntegerFunction()
        {
            // Get(value: out Integer)
            var funcType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Void,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0) }
            );
            var func = module.AddFunction("Get", funcType);
            declaredFunctions["Get"] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = "Get",
                IsBuiltin = true
            };
        }

        private void DeclareGetRealFunction()
        {
            // Get_Real(value: out Real)
            var funcType = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Void,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Double, 0) }
            );
            var func = module.AddFunction("Get_Real", funcType);
            declaredFunctions["Get_Real"] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = "Get_Real",
                IsBuiltin = true
            };
        }

        private void DeclareConversionFunction(string name, LLVMTypeRef[] paramTypes, LLVMTypeRef returnType)
        {
            var funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes);
            var func = module.AddFunction(name, funcType);
            declaredFunctions[name] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = name,
                IsBuiltin = true
            };
        }

        private void DeclareSimpleMathFunction(string name)
        {
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, new[] { LLVMTypeRef.Double });
            var func = module.AddFunction(name, funcType);
            declaredFunctions[name] = new FunctionInfo
            {
                Function = func,
                Type = funcType,
                Name = name,
                IsBuiltin = true
            };
        }

        // === Error Handling ===

        private void AddError(string message, object context = null)
        {
            Errors.Add(new CompilerError
            {
                Message = message,
                Context = context?.GetType().Name ?? "Unknown"
            });
        }

        private void AddWarning(string message, object context = null)
        {
            Warnings.Add(new CompilerWarning
            {
                Message = message,
                Context = context?.GetType().Name ?? "Unknown"
            });
        }

        private bool HasErrors()
        {
            return Errors.Count > 0;
        }

        private bool Validate()
        {
            // Základní validace před generováním kódu
            if (module.Handle == IntPtr.Zero)
            {
                AddError("LLVM modul není inicializován");
                return false;
            }

            return !HasErrors();
        }

        // === Správa scopů ===

        private void EnterScope()
        {
            variableScopes.Push(new Dictionary<string, LLVMValueRef>());
        }

        private void ExitScope()
        {
            if (variableScopes.Count > 0)
            {
                variableScopes.Pop();
            }
        }

        private void AddVariable(string name, LLVMValueRef value, object context = null)
        {
            if (variableScopes.Count == 0)
            {
                EnterScope();
            }

            // Kontrola kolize v aktuálním scope
            if (variableScopes.Peek().ContainsKey(name))
            {
                AddWarning($"Proměnná '{name}' již existuje v tomto scopu a bude přepsána", context);
            }

            // Kontrola kolize s funkcemi
            if (declaredFunctions.ContainsKey(name))
            {
                AddWarning($"Název '{name}' koliduje s existující funkcí", context);
            }

            variableScopes.Peek()[name] = value;
        }

        private bool TryGetVariable(string name, out LLVMValueRef value)
        {
            foreach (var scope in variableScopes)
            {
                if (scope.TryGetValue(name, out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }

        // === Generování kódu ===

        public void Generate(AST.Program program)
        {
            try
            {
                if (!Validate())
                {
                    return;
                }

                GenerateSubprogram(program.MainProcedure);

                // Závěrečná validace
                if (HasErrors())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                AddError($"Neočekávaná chyba při generování kódu: {ex.Message}");
            }
        }

        private void GenerateSubprogram(SubprogramDeclaration subprogram)
        {
            if (subprogram == null)
            {
                AddError("Subprogram je null");
                return;
            }

            EnterScope();

            // Kontrola kolize názvu
            if (declaredFunctions.ContainsKey(subprogram.Name) &&
                !declaredFunctions[subprogram.Name].IsBuiltin)
            {
                AddError($"Funkce '{subprogram.Name}' je již deklarována", subprogram);
                ExitScope();
                return;
            }

            // Určit návratový typ
            LLVMTypeRef returnType;
            if (subprogram is FunctionDeclaration func)
            {
                if (!TryGetLLVMType(func.ReturnType, out returnType, subprogram))
                {
                    ExitScope();
                    return;
                }
            }
            else
            {
                returnType = LLVMTypeRef.Void;
            }

            // Vytvořit typy parametrů
            var paramTypes = new List<LLVMTypeRef>();
            foreach (var param in subprogram.Parameters)
            {
                if (!TryGetLLVMType(param.Type, out var paramType, subprogram))
                {
                    ExitScope();
                    return;
                }

                foreach (var _ in param.Names)
                {
                    paramTypes.Add(paramType);
                }
            }

            // Vytvořit typ funkce
            var functionType = LLVMTypeRef.CreateFunction(returnType, paramTypes.ToArray());

            // Vytvořit funkci
            var function = module.AddFunction(subprogram.Name, functionType);
            currentFunction = function;

            // Uložit informaci o funkci
            declaredFunctions[subprogram.Name] = new FunctionInfo
            {
                Function = function,
                Type = functionType,
                Name = subprogram.Name,
                IsBuiltin = false
            };

            // Vytvořit základní blok
            var entryBlock = function.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBlock);

            // Alokovat parametry
            int paramIndex = 0;
            foreach (var param in subprogram.Parameters)
            {
                if (!TryGetLLVMType(param.Type, out var paramType, subprogram))
                {
                    continue;
                }

                foreach (var name in param.Names)
                {
                    var paramValue = function.GetParam((uint)paramIndex);
                    paramValue.Name = name;

                    // Alokovat prostor pro parametr a uložit hodnotu
                    var alloca = builder.BuildAlloca(paramType, name);
                    builder.BuildStore(paramValue, alloca);
                    AddVariable(name, alloca, subprogram);

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
            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                if (returnType.Kind == LLVMTypeKind.LLVMVoidTypeKind)
                {
                    builder.BuildRetVoid();
                }
                else
                {
                    // Pro funkce bez return vrátit výchozí hodnotu
                    var defaultValue = GetDefaultValue(returnType);
                    builder.BuildRet(defaultValue);
                    AddWarning($"Funkce '{subprogram.Name}' nemá explicitní return statement, použita výchozí hodnota", subprogram);
                }
            }

            ExitScope();
        }

        private void GenerateDeclaration(Declaration decl)
        {
            if (decl == null)
            {
                AddError("Deklarace je null");
                return;
            }

            switch (decl)
            {
                case VariableDeclaration varDecl:
                    foreach (var name in varDecl.Names)
                    {
                        if (!TryGetLLVMType(varDecl.Type, out var type, decl))
                        {
                            continue;
                        }

                        var alloca = builder.BuildAlloca(type, name);
                        AddVariable(name, alloca, decl);

                        if (varDecl.Initializer != null)
                        {
                            var initValue = GenerateExpression(varDecl.Initializer);
                            if (initValue.Handle != IntPtr.Zero)
                            {
                                builder.BuildStore(initValue, alloca);
                            }
                        }
                        else
                        {
                            // Inicializovat na výchozí hodnotu
                            var defaultValue = GetDefaultValue(type);
                            builder.BuildStore(defaultValue, alloca);
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
            if (definition == null)
            {
                AddError($"Definice typu '{name}' je null");
                return;
            }

            // Kontrola kolize názvu typu
            if (namedTypes.ContainsKey(name))
            {
                AddWarning($"Typ '{name}' je již definován a bude přepsán", definition);
            }

            if (definition is ArrayTypeDefinition arrayDef)
            {
                if (!TryGetLLVMType(arrayDef.ElementType, out var elementType, definition))
                {
                    return;
                }

                // Vypočítat celkovou velikost pole pro každou dimenzi
                var dimensions = new List<uint>();
                foreach (var range in arrayDef.IndexRanges)
                {
                    // Zjednodušení: předpokládáme konstantní rozsahy
                    if (range.Lower is IntegerLiteral lower && range.Upper is IntegerLiteral upper)
                    {
                        if (upper.Value < lower.Value)
                        {
                            AddError($"Neplatný rozsah pole: horní hranice ({upper.Value}) je menší než dolní hranice ({lower.Value})", definition);
                            return;
                        }

                        uint size = (uint)(upper.Value - lower.Value + 1);
                        dimensions.Add(size);
                    }
                    else
                    {
                        AddError($"Dynamické rozsahy polí nejsou podporovány", definition);
                        return;
                    }
                }

                // Vytvořit typ pole - vnořené pole pro vícerozměrná pole
                LLVMTypeRef arrayType = elementType;
                for (int i = dimensions.Count - 1; i >= 0; i--)
                {
                    arrayType = LLVMTypeRef.CreateArray(arrayType, dimensions[i]);
                }

                namedTypes[name] = arrayType;
                arrayDefinitions[name] = arrayDef;
            }
        }

        private void GenerateStatement(Statement stmt)
        {
            if (stmt == null)
            {
                AddWarning("Statement je null, přeskakuji");
                return;
            }

            // Pokud blok už má terminátor, přeskočit další příkazy
            if (builder.InsertBlock.Terminator.Handle != IntPtr.Zero)
            {
                return;
            }

            switch (stmt)
            {
                case AssignmentStatement assign:
                    var target = GetVariablePointer(assign.Target);
                    if (target.Handle == IntPtr.Zero)
                    {
                        break;
                    }

                    var value = GenerateExpression(assign.Value);
                    if (value.Handle == IntPtr.Zero)
                    {
                        break;
                    }

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
                        if (retValue.Handle != IntPtr.Zero)
                        {
                            builder.BuildRet(retValue);
                        }
                    }
                    else
                    {
                        builder.BuildRetVoid();
                    }
                    break;

                case ExitStatement exit:
                    if (loopExitBlocks.Count > 0)
                    {
                        builder.BuildBr(loopExitBlocks.Peek());
                    }
                    else
                    {
                        AddError("Exit příkaz mimo loop", exit);
                    }
                    break;
            }
        }

        private void GenerateIfStatement(IfStatement ifStmt)
        {
            var condition = GenerateExpression(ifStmt.Condition);
            if (condition.Handle == IntPtr.Zero)
            {
                return;
            }

            var condBool = builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condition,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false), "ifcond");

            var thenBlock = currentFunction.AppendBasicBlock("then");
            var mergeBlock = currentFunction.AppendBasicBlock("ifcont");

            // Pokud jsou elsif nebo else větve, vytvoříme další bloky
            LLVMBasicBlockRef elseBlock;
            if (ifStmt.ElsifClauses.Count > 0 || ifStmt.ElseStatements.Count > 0)
            {
                elseBlock = currentFunction.AppendBasicBlock("else");
            }
            else
            {
                elseBlock = mergeBlock;
            }

            builder.BuildCondBr(condBool, thenBlock, elseBlock);

            // Then blok
            builder.PositionAtEnd(thenBlock);
            foreach (var stmt in ifStmt.ThenStatements)
            {
                GenerateStatement(stmt);
            }
            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                builder.BuildBr(mergeBlock);
            }

            // Elsif a else bloky
            if (ifStmt.ElsifClauses.Count > 0 || ifStmt.ElseStatements.Count > 0)
            {
                builder.PositionAtEnd(elseBlock);

                // Zpracovat elsif klauzule
                for (int i = 0; i < ifStmt.ElsifClauses.Count; i++)
                {
                    var elsif = ifStmt.ElsifClauses[i];
                    var elsifCond = GenerateExpression(elsif.Condition);
                    if (elsifCond.Handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var elsifCondBool = builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, elsifCond,
                        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false), "elsifcond");

                    var elsifThen = currentFunction.AppendBasicBlock("elsif_then");
                    LLVMBasicBlockRef nextElsif;

                    if (i < ifStmt.ElsifClauses.Count - 1 || ifStmt.ElseStatements.Count > 0)
                    {
                        nextElsif = currentFunction.AppendBasicBlock("next_elsif");
                    }
                    else
                    {
                        nextElsif = mergeBlock;
                    }

                    builder.BuildCondBr(elsifCondBool, elsifThen, nextElsif);

                    builder.PositionAtEnd(elsifThen);
                    foreach (var stmt in elsif.Statements)
                    {
                        GenerateStatement(stmt);
                    }
                    if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
                    {
                        builder.BuildBr(mergeBlock);
                    }

                    builder.PositionAtEnd(nextElsif);
                }

                // Zpracovat else větev
                if (ifStmt.ElseStatements.Count > 0)
                {
                    foreach (var stmt in ifStmt.ElseStatements)
                    {
                        GenerateStatement(stmt);
                    }
                }

                if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
                {
                    builder.BuildBr(mergeBlock);
                }
            }

            builder.PositionAtEnd(mergeBlock);
        }

        private void GenerateLoopStatement(LoopStatement loop)
        {
            var loopBlock = currentFunction.AppendBasicBlock("loop");
            var afterBlock = currentFunction.AppendBasicBlock("afterloop");

            // Přidat exit point pro tento loop
            loopExitBlocks.Push(afterBlock);

            builder.BuildBr(loopBlock);
            builder.PositionAtEnd(loopBlock);

            foreach (var stmt in loop.Statements)
            {
                GenerateStatement(stmt);
            }

            // Pokud blok nemá terminátor, přidat skok zpět na začátek
            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                builder.BuildBr(loopBlock);
            }

            loopExitBlocks.Pop();
            builder.PositionAtEnd(afterBlock);
        }

        private void GenerateForStatement(ForStatement forStmt)
        {
            EnterScope();

            // Alokovat iterátor
            var iteratorAlloca = builder.BuildAlloca(LLVMTypeRef.Int32, forStmt.Iterator);
            AddVariable(forStmt.Iterator, iteratorAlloca, forStmt);

            // Vyhodnotit hranice
            var startValue = GenerateExpression(forStmt.LowerBound);
            var endValue = GenerateExpression(forStmt.UpperBound);

            if (startValue.Handle == IntPtr.Zero || endValue.Handle == IntPtr.Zero)
            {
                ExitScope();
                return;
            }

            // Uložit do lokálních proměnných
            var endAlloca = builder.BuildAlloca(LLVMTypeRef.Int32, "loop_end");
            builder.BuildStore(endValue, endAlloca);

            // Inicializovat iterátor
            builder.BuildStore(startValue, iteratorAlloca);

            var condBlock = currentFunction.AppendBasicBlock("for_cond");
            var loopBlock = currentFunction.AppendBasicBlock("for_body");
            var afterBlock = currentFunction.AppendBasicBlock("for_end");

            // Přidat exit point
            loopExitBlocks.Push(afterBlock);

            builder.BuildBr(condBlock);

            // Podmínka
            builder.PositionAtEnd(condBlock);
            var currentValue = builder.BuildLoad2(LLVMTypeRef.Int32, iteratorAlloca, "iter");
            var endVal = builder.BuildLoad2(LLVMTypeRef.Int32, endAlloca, "end");

            LLVMValueRef condition;
            if (forStmt.Reverse)
            {
                condition = builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, currentValue, endVal, "loopcond");
            }
            else
            {
                condition = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentValue, endVal, "loopcond");
            }

            builder.BuildCondBr(condition, loopBlock, afterBlock);

            // Tělo cyklu
            builder.PositionAtEnd(loopBlock);
            foreach (var stmt in forStmt.Statements)
            {
                GenerateStatement(stmt);
            }

            // Inkrementace/dekrementace
            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                var iterValue = builder.BuildLoad2(LLVMTypeRef.Int32, iteratorAlloca, "iter");
                var nextValue = forStmt.Reverse
                    ? builder.BuildSub(iterValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false), "next")
                    : builder.BuildAdd(iterValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false), "next");
                builder.BuildStore(nextValue, iteratorAlloca);

                builder.BuildBr(condBlock);
            }

            loopExitBlocks.Pop();
            builder.PositionAtEnd(afterBlock);

            ExitScope();
        }

        private void GenerateProcedureCall(ProcedureCallStatement call)
        {
            if (!declaredFunctions.TryGetValue(call.Name, out var funcInfo))
            {
                AddError($"Funkce '{call.Name}' nebyla deklarována", call);
                return;
            }

            var args = call.Arguments.Select(GenerateExpression).ToArray();

            // Kontrola počtu argumentů
            if (funcInfo.Type.ParamTypesCount != args.Length)
            {
                AddWarning($"Funkce '{call.Name}' očekává {funcInfo.Type.ParamTypesCount} argumentů, ale bylo poskytnuto {args.Length}", call);
            }

            // Filtrovat neplatné argumenty
            if (args.Any(a => a.Handle == IntPtr.Zero))
            {
                AddError($"Některé argumenty volání '{call.Name}' jsou neplatné", call);
                return;
            }

            // Pro builtin funkce použít speciální generování
            if (funcInfo.IsBuiltin)
            {
                GenerateBuiltinCall(funcInfo, args);
            }
            else
            {
                builder.BuildCall2(funcInfo.Type, funcInfo.Function, args, "");
            }
        }

        private void GenerateBuiltinCall(FunctionInfo funcInfo, LLVMValueRef[] args)
        {
            var printf = declaredFunctions["printf"].Function;
            var sprintf = declaredFunctions["sprintf"].Function;
            var scanf = declaredFunctions["scanf"].Function;
            var fgets = declaredFunctions["fgets"].Function;
            var stdin = module.GetNamedGlobal("stdin");

            switch (funcInfo.Name)
            {
                case "Put_Line":
                    if (args.Length > 0)
                    {
                        var format = builder.BuildGlobalStringPtr("%s\n", "fmt");
                        builder.BuildCall2(printf.TypeOf.ElementType, printf, new[] { format, args[0] }, "");
                    }
                    break;

                case "Put":
                    if (args.Length > 0)
                    {
                        var format = builder.BuildGlobalStringPtr("%s", "fmt");
                        builder.BuildCall2(printf.TypeOf.ElementType, printf, new[] { format, args[0] }, "");
                    }
                    break;

                case "Put_Integer":
                    if (args.Length > 0)
                    {
                        var format = builder.BuildGlobalStringPtr("%d", "fmt");
                        builder.BuildCall2(printf.TypeOf.ElementType, printf, new[] { format, args[0] }, "");
                    }
                    break;

                case "Put_Real":
                    if (args.Length > 0)
                    {
                        var format = builder.BuildGlobalStringPtr("%f", "fmt");
                        builder.BuildCall2(printf.TypeOf.ElementType, printf, new[] { format, args[0] }, "");
                    }
                    break;

                case "New_Line":
                    var nlFormat = builder.BuildGlobalStringPtr("\n", "fmt");
                    builder.BuildCall2(printf.TypeOf.ElementType, printf, new[] { nlFormat }, "");
                    break;

                case "Get_Line":
                    // fgets(buffer, size, stdin)
                    if (args.Length > 0)
                    {
                        var bufferSize = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 256, false);
                        var stdinPtr = builder.BuildLoad2(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), stdin, "stdin");
                        builder.BuildCall2(fgets.TypeOf.ElementType, fgets,
                            new[] { args[0], bufferSize, stdinPtr }, "");
                    }
                    break;

                case "Get":
                    // scanf("%d", &value)
                    if (args.Length > 0)
                    {
                        var format = builder.BuildGlobalStringPtr("%d", "scanf_fmt");
                        builder.BuildCall2(scanf.TypeOf.ElementType, scanf,
                            new[] { format, args[0] }, "");
                    }
                    break;

                case "Get_Real":
                    // scanf("%lf", &value)
                    if (args.Length > 0)
                    {
                        var format = builder.BuildGlobalStringPtr("%lf", "scanf_fmt");
                        builder.BuildCall2(scanf.TypeOf.ElementType, scanf,
                            new[] { format, args[0] }, "");
                    }
                    break;
            }
        }

        private LLVMValueRef GenerateExpression(Expression expr)
        {
            if (expr == null)
            {
                AddError("Výraz je null");
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

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
                    if (ptr.Handle == IntPtr.Zero)
                    {
                        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
                    }

                    if (!TryGetVariableType(variable, out var varType))
                    {
                        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
                    }

                    return builder.BuildLoad2(varType, ptr, variable.Name);

                case BinaryExpression binary:
                    return GenerateBinaryExpression(binary);

                case UnaryExpression unary:
                    return GenerateUnaryExpression(unary);

                case FunctionCall funcCall:
                    return GenerateFunctionCall(funcCall);

                default:
                    AddError($"Nepodporovaný typ výrazu: {expr.GetType()}", expr);
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }
        }

        private LLVMValueRef GenerateFunctionCall(FunctionCall funcCall)
        {
            // Nejprve zkontrolovat, zda to není konverzní funkce
            if (IsConversionFunction(funcCall.Name))
            {
                return GenerateConversionCall(funcCall);
            }

            // Pak zkontrolovat matematické funkce
            if (IsMathFunction(funcCall.Name))
            {
                return GenerateMathCall(funcCall);
            }

            // Běžné volání funkce
            if (!declaredFunctions.TryGetValue(funcCall.Name, out var funcInfo))
            {
                AddError($"Funkce '{funcCall.Name}' nebyla deklarována", funcCall);
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            var funcArgs = funcCall.Arguments.Select(GenerateExpression).ToArray();

            // Kontrola neplatných argumentů
            if (funcArgs.Any(a => a.Handle == IntPtr.Zero))
            {
                AddError($"Některé argumenty volání '{funcCall.Name}' jsou neplatné", funcCall);
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            return builder.BuildCall2(funcInfo.Type, funcInfo.Function, funcArgs, "calltmp");
        }

        private bool IsConversionFunction(string name)
        {
            return name == "Integer_To_String" || name == "Real_To_String" ||
                   name == "String_To_Integer" || name == "String_To_Real" ||
                   name == "Integer_To_Real" || name == "Real_To_Integer";
        }

        private bool IsMathFunction(string name)
        {
            return name == "sqrt" || name == "sin" || name == "cos" ||
                   name == "tan" || name == "exp" || name == "log" || name == "pow";
        }

        private LLVMValueRef GenerateConversionCall(FunctionCall funcCall)
        {
            var args = funcCall.Arguments.Select(GenerateExpression).ToArray();

            if (args.Any(a => a.Handle == IntPtr.Zero))
            {
                AddError($"Neplatné argumenty pro konverzní funkci '{funcCall.Name}'", funcCall);
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            switch (funcCall.Name)
            {
                case "Integer_To_String":
                    // Alokovat buffer pro string
                    var intBuffer = builder.BuildAlloca(
                        LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, 32), "int_str_buf");
                    var intBufferPtr = builder.BuildBitCast(intBuffer,
                        LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), "bufptr");

                    var intFmt = builder.BuildGlobalStringPtr("%d", "int_fmt");
                    var sprintf = declaredFunctions["sprintf"].Function;
                    builder.BuildCall2(sprintf.TypeOf.ElementType, sprintf,
                        new[] { intBufferPtr, intFmt, args[0] }, "");
                    return intBufferPtr;

                case "Real_To_String":
                    // Alokovat buffer pro string
                    var realBuffer = builder.BuildAlloca(
                        LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, 32), "real_str_buf");
                    var realBufferPtr = builder.BuildBitCast(realBuffer,
                        LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), "bufptr");

                    var realFmt = builder.BuildGlobalStringPtr("%f", "real_fmt");
                    var sprintfReal = declaredFunctions["sprintf"].Function;
                    builder.BuildCall2(sprintfReal.TypeOf.ElementType, sprintfReal,
                        new[] { realBufferPtr, realFmt, args[0] }, "");
                    return realBufferPtr;

                case "String_To_Integer":
                    var atoi = declaredFunctions["atoi"].Function;
                    return builder.BuildCall2(atoi.TypeOf.ElementType, atoi, args, "strtoint");

                case "String_To_Real":
                    var atof = declaredFunctions["atof"].Function;
                    return builder.BuildCall2(atof.TypeOf.ElementType, atof, args, "strtoreal");

                case "Integer_To_Real":
                    return builder.BuildSIToFP(args[0], LLVMTypeRef.Double, "i2r");

                case "Real_To_Integer":
                    return builder.BuildFPToSI(args[0], LLVMTypeRef.Int32, "r2i");

                default:
                    AddError($"Neznámá konverzní funkce: {funcCall.Name}", funcCall);
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }
        }

        private LLVMValueRef GenerateMathCall(FunctionCall funcCall)
        {
            if (!declaredFunctions.TryGetValue(funcCall.Name, out var mathFunc))
            {
                AddError($"Matematická funkce '{funcCall.Name}' nebyla deklarována", funcCall);
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            var args = funcCall.Arguments.Select(GenerateExpression).ToArray();

            if (args.Any(a => a.Handle == IntPtr.Zero))
            {
                AddError($"Neplatné argumenty pro matematickou funkci '{funcCall.Name}'", funcCall);
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // Konvertovat argumenty na double pokud jsou integer
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
                {
                    args[i] = builder.BuildSIToFP(args[i], LLVMTypeRef.Double, "mathargtofp");
                }
            }

            return builder.BuildCall2(mathFunc.Type, mathFunc.Function, args, "mathcall");
        }

        private LLVMValueRef GenerateBinaryExpression(BinaryExpression binary)
        {
            var left = GenerateExpression(binary.Left);
            var right = GenerateExpression(binary.Right);

            if (left.Handle == IntPtr.Zero || right.Handle == IntPtr.Zero)
            {
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // Konverze typů pokud je potřeba
            if (left.TypeOf.Kind != right.TypeOf.Kind)
            {
                if (left.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind && right.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind)
                {
                    left = builder.BuildSIToFP(left, LLVMTypeRef.Double, "inttofp");
                }
                else if (left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind && right.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
                {
                    right = builder.BuildSIToFP(right, LLVMTypeRef.Double, "inttofp");
                }
            }

            bool isFloat = left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind;

            switch (binary.Operator)
            {
                case BinaryOperator.Add:
                    return isFloat ? builder.BuildFAdd(left, right, "addtmp") : builder.BuildAdd(left, right, "addtmp");

                case BinaryOperator.Subtract:
                    return isFloat ? builder.BuildFSub(left, right, "subtmp") : builder.BuildSub(left, right, "subtmp");

                case BinaryOperator.Multiply:
                    return isFloat ? builder.BuildFMul(left, right, "multmp") : builder.BuildMul(left, right, "multmp");

                case BinaryOperator.Divide:
                    return isFloat ? builder.BuildFDiv(left, right, "divtmp") : builder.BuildSDiv(left, right, "divtmp");

                case BinaryOperator.Mod:
                    return builder.BuildSRem(left, right, "modtmp");

                case BinaryOperator.Power:
                    // Pro mocninu použijeme pow funkci
                    var powFunc = declaredFunctions["pow"].Function;

                    if (!isFloat)
                    {
                        left = builder.BuildSIToFP(left, LLVMTypeRef.Double, "powleft");
                        right = builder.BuildSIToFP(right, LLVMTypeRef.Double, "powright");
                    }

                    return builder.BuildCall2(powFunc.TypeOf.ElementType, powFunc, new[] { left, right }, "powtmp");

                case BinaryOperator.Equal:
                    return isFloat
                        ? builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, left, right, "eqtmp")
                        : builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eqtmp");

                case BinaryOperator.NotEqual:
                    return isFloat
                        ? builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, left, right, "netmp")
                        : builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "netmp");

                case BinaryOperator.Less:
                    return isFloat
                        ? builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, left, right, "lttmp")
                        : builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lttmp");

                case BinaryOperator.LessOrEqual:
                    return isFloat
                        ? builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, left, right, "letmp")
                        : builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "letmp");

                case BinaryOperator.Greater:
                    return isFloat
                        ? builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, left, right, "gttmp")
                        : builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gttmp");

                case BinaryOperator.GreaterOrEqual:
                    return isFloat
                        ? builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, left, right, "getmp")
                        : builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "getmp");

                case BinaryOperator.And:
                    return builder.BuildAnd(left, right, "andtmp");

                case BinaryOperator.Or:
                    return builder.BuildOr(left, right, "ortmp");

                default:
                    AddError($"Nepodporovaný binární operátor: {binary.Operator}", binary);
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }
        }

        private LLVMValueRef GenerateUnaryExpression(UnaryExpression unary)
        {
            var operand = GenerateExpression(unary.Operand);

            if (operand.Handle == IntPtr.Zero)
            {
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

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
                    AddError($"Nepodporovaný unární operátor: {unary.Operator}", unary);
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }
        }

        private LLVMValueRef GetVariablePointer(Variable variable)
        {
            if (!TryGetVariable(variable.Name, out var value))
            {
                AddError($"Proměnná '{variable.Name}' nebyla nalezena", variable);
                return default;
            }

            if (variable.ArrayIndices != null && variable.ArrayIndices.Count > 0)
            {
                return GetArrayElementPointer(variable, value);
            }

            return value;
        }

        private LLVMValueRef GetArrayElementPointer(Variable variable, LLVMValueRef arrayPtr)
        {
            // Zjistit typ pole
            var arrayPtrType = arrayPtr.TypeOf;
            var arrayType = arrayPtrType.ElementType;

            // Najít definici pole pro získání rozsahů
            ArrayTypeDefinition arrayDef = null;

            // Hledat v namedTypes
            foreach (var kvp in arrayDefinitions)
            {
                if (namedTypes.TryGetValue(kvp.Key, out var type) && type.Handle == arrayType.Handle)
                {
                    arrayDef = kvp.Value;
                    break;
                }
            }

            if (arrayDef == null)
            {
                // Fallback - použít jednoduchý přístup bez úpravy indexů
                var indices = new List<LLVMValueRef> {
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false)
                };
                indices.AddRange(variable.ArrayIndices.Select(GenerateExpression));

                if (indices.Any(i => i.Handle == IntPtr.Zero))
                {
                    AddError($"Neplatné indexy pole pro '{variable.Name}'", variable);
                    return default;
                }

                return builder.BuildGEP2(arrayType, arrayPtr, indices.ToArray(), "arrayidx");
            }

            // Kontrola počtu dimenzí
            if (variable.ArrayIndices.Count != arrayDef.IndexRanges.Count)
            {
                AddError($"Pole '{variable.Name}' má {arrayDef.IndexRanges.Count} dimenzí, ale bylo poskytnuto {variable.ArrayIndices.Count} indexů", variable);
                return default;
            }

            // Vytvoření indexů s úpravou na 0-based
            var adjustedIndices = new List<LLVMValueRef> {
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false)
            };

            for (int i = 0; i < variable.ArrayIndices.Count; i++)
            {
                var index = GenerateExpression(variable.ArrayIndices[i]);
                if (index.Handle == IntPtr.Zero)
                {
                    AddError($"Neplatný index {i} pole '{variable.Name}'", variable);
                    return default;
                }

                var range = arrayDef.IndexRanges[i];

                // Ada používá custom rozsahy (např. 1..10), musíme převést na 0-based
                if (range.Lower is IntegerLiteral lowerLit)
                {
                    if (lowerLit.Value != 0)
                    {
                        var lowerBound = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)lowerLit.Value, true);
                        index = builder.BuildSub(index, lowerBound, "adjusted_idx");
                    }
                }

                adjustedIndices.Add(index);
            }

            return builder.BuildGEP2(arrayType, arrayPtr, adjustedIndices.ToArray(), "arrayptr");
        }

        private bool TryGetVariableType(Variable variable, out LLVMTypeRef type)
        {
            if (!TryGetVariable(variable.Name, out var ptr))
            {
                type = default;
                return false;
            }

            type = ptr.TypeOf.ElementType;

            // Pro pole vrátit typ prvku
            if (variable.ArrayIndices != null && variable.ArrayIndices.Count > 0)
            {
                for (int i = 0; i < variable.ArrayIndices.Count; i++)
                {
                    if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
                    {
                        type = type.ElementType;
                    }
                }
            }

            return true;
        }

        private bool TryGetLLVMType(TypeName typeName, out LLVMTypeRef type, object context = null)
        {
            if (namedTypes.TryGetValue(typeName.Name, out type))
            {
                return true;
            }

            AddError($"Typ '{typeName.Name}' nebyl nalezen", context);
            type = default;
            return false;
        }

        private LLVMValueRef GetDefaultValue(LLVMTypeRef type)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    return LLVMValueRef.CreateConstInt(type, 0, false);
                case LLVMTypeKind.LLVMDoubleTypeKind:
                    return LLVMValueRef.CreateConstReal(type, 0.0);
                case LLVMTypeKind.LLVMPointerTypeKind:
                    return LLVMValueRef.CreateConstPointerNull(type);
                default:
                    return LLVMValueRef.CreateConstNull(type);
            }
        }

        public void WriteToFile(string filename)
        {
            if (HasErrors())
            {
                throw new Exception($"Nelze zapsat soubor, generování obsahuje {Errors.Count} chyb");
            }

            module.PrintToFile(filename);
        }

        public void WriteBitcodeToFile(string filename)
        {
            if (HasErrors())
            {
                throw new Exception($"Nelze zapsat bitcode, generování obsahuje {Errors.Count} chyb");
            }

            module.WriteBitcodeToFile(filename);
        }

        public string PrintToString()
        {
            return module.PrintToString();
        }

        public void PrintErrors()
        {
            if (Errors.Count > 0)
            {
                Console.WriteLine($"\n=== CHYBY ({Errors.Count}) ===");
                foreach (var error in Errors)
                {
                    Console.WriteLine(error.ToString());
                }
            }

            if (Warnings.Count > 0)
            {
                Console.WriteLine($"\n=== VAROVÁNÍ ({Warnings.Count}) ===");
                foreach (var warning in Warnings)
                {
                    Console.WriteLine(warning.ToString());
                }
            }
        }
    }

    // Pomocná třída pro informace o funkcích
    internal class FunctionInfo
    {
        public LLVMValueRef Function { get; set; }
        public LLVMTypeRef Type { get; set; }
        public string Name { get; set; }
        public bool IsBuiltin { get; set; }
        public bool NewLine { get; set; }
    }

    // Pomocné třídy pro error handling
    public class CompilerError
    {
        public string Message { get; set; }
        public string Context { get; set; }

        public override string ToString()
        {
            return $"CHYBA [{Context}]: {Message}";
        }
    }

    public class CompilerWarning
    {
        public string Message { get; set; }
        public string Context { get; set; }

        public override string ToString()
        {
            return $"VAROVÁNÍ [{Context}]: {Message}";
        }
    }
}