using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace BTEJA_SemestralniPrace
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Pokud je zadán konkrétní soubor
            if (args.Length > 0)
            {
                AnalyzeSingleFile(args[0]);
                return;
            }

            // Jinak testuj všechny příklady
            TestAllExamples();
        }

        static void AnalyzeSingleFile(string filePath)
        {
            Console.WriteLine($"Analýza souboru: {filePath}");

            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Soubor '{filePath}' neexistuje!");
                Console.ResetColor();
                return;
            }

            string sourceCode = File.ReadAllText(filePath);
            var errors = AnalyzeSource(sourceCode);

            if (errors.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ÚSPĚCH");
                Console.ResetColor();
                Console.WriteLine("Frontend zpracoval soubor bez chyb:");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NEÚSPĚCH");
                Console.ResetColor();
                Console.WriteLine("Nalezené chyby:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"- {error}");
                }
            }
        }

        static void TestAllExamples()
        {
            Console.WriteLine("Testování všech příkladů");
            Console.WriteLine();

            // Najdi všechny .ada soubory
            string[] exampleFiles;

            if (Directory.Exists("examples"))
            {
                exampleFiles = Directory.GetFiles("examples", "*.ada");
            }
            else
            {
                exampleFiles = Directory.GetFiles(".", "*.ada");
            }

            if (exampleFiles.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Nenalezeny žádné .ada soubory!");
                Console.ResetColor();
                Console.WriteLine("Hledané umístění:");
                Console.WriteLine("- ./examples/*.ada");
                Console.WriteLine("- ./*.ada");
                return;
            }

            // Seřaď soubory podle jména
            Array.Sort(exampleFiles);

            int totalTests = exampleFiles.Length;
            int passedTests = 0;
            int failedTests = 0;

            Console.WriteLine($"Nalezeno {totalTests} příkladů:");
            Console.WriteLine();

            // Testuj každý příklad
            for (int i = 0; i < exampleFiles.Length; i++)
            {
                string filePath = exampleFiles[i];
                string fileName = Path.GetFileName(filePath);

                Console.WriteLine($"[{i + 1}/{totalTests}] {fileName}");

                string sourceCode = File.ReadAllText(filePath);
                var errors = AnalyzeSource(sourceCode);

                if (errors.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("PASS - Sémantická analýza prošla bez chyb");
                    Console.ResetColor();
                    passedTests++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FAIL - Nalezeny chyby:");
                    Console.ResetColor();
                    foreach (var error in errors)
                    {
                        Console.WriteLine($" - {error}");
                    }
                    failedTests++;
                }

                Console.WriteLine();
            }

            // Výsledky
            Console.WriteLine("VÝSLEDKY TESTOVÁNÍ");
            Console.WriteLine($"Celkem testů: {totalTests}");

            if (passedTests > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Úspěšných: {passedTests}");
                Console.ResetColor();
            }

            if (failedTests > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Neúspěšných: {failedTests}");
                Console.ResetColor();
            }

            if (failedTests == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("VŠECHNY TESTY PROŠLY ÚSPĚŠNĚ!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("NĚKTERÉ PŘÍKLADY OBSAHUJÍ CHYBY!");
                Console.ResetColor();
            }
        }

        static List<string> AnalyzeSource(string sourceCode)
        {
            var errors = new List<string>();

            try
            {
                // 1. Lexikální analýza
                var inputStream = new AntlrInputStream(sourceCode);
                var lexer = new AdaLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);

                // 2. Syntaktická analýza
                var parser = new AdaParser(tokenStream);

                // Error listener
                parser.RemoveErrorListeners();
                var errorListener = new ErrorListener();
                parser.AddErrorListener(errorListener);

                var parseTree = parser.program();

                if (errorListener.Errors.Count > 0)
                {
                    errors.AddRange(errorListener.Errors);
                    return errors;
                }

                // 3. Vytvoření AST
                var astBuilder = new ASTBuilder();
                var program = (BTEJA_SemestralniPrace.AST.Program)astBuilder.VisitProgram(parseTree);

                // 4. Sémantická analýza
                var semanticAnalyzer = new SemanticAnalyzer();
                semanticAnalyzer.Analyze(program);

                if (semanticAnalyzer.Errors.Count > 0)
                {
                    errors.AddRange(semanticAnalyzer.Errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Interní chyba: {ex.Message}");
            }

            return errors;
        }
    }

    // Error listener pro ANTLR
    class ErrorListener : BaseErrorListener
    {
        public List<string> Errors { get; } = new List<string>();

        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            Errors.Add($"Řádek {line}, Sloupec {charPositionInLine}: Syntaktická chyba - {msg}");
        }
    }
}