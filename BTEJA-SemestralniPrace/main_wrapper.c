/**
 * Main Wrapper for Ada Compiler
 * Poskytuje C entry point, který volá Ada Main proceduru
 */

#include <stdio.h>
#include <stdlib.h>

// ============================================================================
// DEKLARACE EXTERNÍ Ada Main PROCEDURY
// ============================================================================

/**
 * Deklarace Main procedury z Ada programu
 * Tato procedura je generována LLVM code generatorem jako "Main"
 */
extern void Main(void);

// ============================================================================
// C ENTRY POINT
// ============================================================================

/**
 * Hlavní C entry point programu
 *
 * Tento wrapper:
 * 1. Inicializuje C runtime prostředí
 * 2. Volá Ada Main proceduru
 * 3. Vrací exit code
 *
 * @param argc Počet argumentů příkazové řádky
 * @param argv Pole argumentů příkazové řádky
 * @return Exit code (0 = úspěch)
 */
int main(int argc, char* argv[]) {
    // Potenciální budoucí použití command-line argumentů
    // Pro budoucí rozšíření Ada runtime můžeme předat argc/argv
    (void)argc;  // Suppress unused warning
    (void)argv;  // Suppress unused warning

    // Volání Ada Main procedury
    Main();

    // Úspěšné ukončení
    return 0;
}

// ============================================================================
// VOLITELNÉ: EXCEPTION HANDLING (pro budoucí rozšíření)
// ============================================================================

/**
 * Ada Runtime Exception Handler (placeholder)
 * V budoucnu lze rozšířit o proper exception handling
 */
void __ada_runtime_error(const char* message) {
    fprintf(stderr, "RUNTIME ERROR: %s\n", message);
    exit(1);
}

/**
 * Ada Constraint Error Handler
 */
void __ada_constraint_error(const char* message) {
    fprintf(stderr, "CONSTRAINT ERROR: %s\n", message);
    exit(1);
}

/**
 * Ada Program Error Handler
 */
void __ada_program_error(const char* message) {
    fprintf(stderr, "PROGRAM ERROR: %s\n", message);
    exit(1);
}

// ============================================================================
// VOLITELNÉ: FINALIZATION (pro budoucí rozšíření)
// ============================================================================

/**
 * Ada Runtime Finalization
 * Volá se automaticky při ukončení programu
 */
void __attribute__((destructor)) ada_runtime_finalize(void) {
    // Placeholder pro budoucí cleanup kód
    // Např. zavření otevřených souborů, uvolnění resources, atd.
}