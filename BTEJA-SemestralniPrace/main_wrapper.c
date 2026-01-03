/**
 * Universal Main Wrapper for Ada Compiler
 *
 * Tento wrapper automaticky volá hlavní proceduru Ada programu bez ohledu na její název.
 * Název procedury je definován v MAIN_PROCEDURE_NAME během kompilace.
 */

#include <stdio.h>
#include <stdlib.h>

 // ============================================================================
 // KONFIGURACE - NÁZEV HLAVNÍ PROCEDURY
 // ============================================================================

 /**
  * Název hlavní procedury je definován kompilátorem pomocí -D flag
  * Příklad: -DMAIN_PROCEDURE_NAME=Matrix_Operations
  *
  * Pokud není definován, použije se výchozí "Main"
  */
#ifndef MAIN_PROCEDURE_NAME
#define MAIN_PROCEDURE_NAME Main
#endif

  // Pomocné makro pro vytvoření deklarace funkce
#define DECLARE_MAIN_PROC(name) extern void name(void)
#define CALL_MAIN_PROC(name) name()

// Deklarace hlavní procedury s dynamickým názvem
DECLARE_MAIN_PROC(MAIN_PROCEDURE_NAME);

// ============================================================================
// C ENTRY POINT
// ============================================================================

/**
 * Hlavní C entry point programu
 *
 * Tento wrapper:
 * 1. Inicializuje C runtime prostředí
 * 2. Volá Ada hlavní proceduru (s libovolným názvem)
 * 3. Vrací exit code
 *
 * @param argc Počet argumentů příkazové řádky
 * @param argv Pole argumentů příkazové řádky
 * @return Exit code (0 = úspěch)
 */
int main(int argc, char* argv[]) {
    // Potenciální budoucí použití command-line argumentů
    (void)argc;  // Suppress unused warning
    (void)argv;  // Suppress unused warning

    // Volání Ada hlavní procedury (název je definován při kompilaci)
    CALL_MAIN_PROC(MAIN_PROCEDURE_NAME);

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