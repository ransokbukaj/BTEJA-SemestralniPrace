/**
* Runtime Library for Ada Compiler
* Implements all built-in functions required by the LLVM code generator
*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <time.h>
#include <ctype.h>

// ============================================================================
// KONSTANTY A BUFFERY
// ============================================================================

#define MAX_STRING_LENGTH 1024
#define MAX_INPUT_LENGTH 256

// Globální buffer pro konverze
static char global_string_buffer[MAX_STRING_LENGTH];
static int string_buffer_initialized = 0;

// ============================================================================
// INICIALIZACE
// ============================================================================

static void init_runtime() {
    if (!string_buffer_initialized) {
        srand(time(NULL));
        string_buffer_initialized = 1;
    }
}

// ============================================================================
// VSTUP/VÝSTUP - Textové funkce
// ============================================================================

/**
 * Put_Line - Vypíše řetězec s novým řádkem
 */
void Put_Line(const char* str) {
    init_runtime();
    if (str) {
        printf("%s\n", str);
    }
}

/**
 * Put - Vypíše řetězec bez nového řádku
 */
void Put(const char* str) {
    init_runtime();
    if (str) {
        printf("%s", str);
    }
}

/**
 * Put_Integer - Vypíše celé číslo
 */
void Put_Integer(int value) {
    printf("%d", value);
}

/**
 * Put_Real - Vypíše reálné číslo
 */
void Put_Real(double value) {
    printf("%g", value);
}

/**
 * New_Line - Vypíše nový řádek
 */
void New_Line() {
    printf("\n");
}

// ============================================================================
// VSTUP/VÝSTUP - Vstupní funkce
// ============================================================================

/**
 * Get_Line - Načte řádek textu ze stdin
 */
void Get_Line(char* buffer) {
    if (!buffer) return;

    if (fgets(buffer, MAX_INPUT_LENGTH, stdin)) {
        // Odstranit trailing newline
        size_t len = strlen(buffer);
        if (len > 0 && buffer[len - 1] == '\n') {
            buffer[len - 1] = '\0';
        }
    }
}

/**
 * Get - Načte celé číslo ze stdin
 */
void Get(int* value) {
    if (!value) return;
    scanf("%d", value);
    // Vyčistit buffer
    int c;
    while ((c = getchar()) != '\n' && c != EOF);
}

/**
 * Get_Real - Načte reálné číslo ze stdin
 */
void Get_Real(double* value) {
    if (!value) return;
    scanf("%lf", value);
    // Vyčistit buffer
    int c;
    while ((c = getchar()) != '\n' && c != EOF);
}

// ============================================================================
// KONVERZNÍ FUNKCE - Integer ↔ String
// ============================================================================

/**
 * Integer_To_String - Převede integer na string
 */
char* Integer_To_String(int value) {
    static char buffer[64];
    snprintf(buffer, sizeof(buffer), "%d", value);
    return buffer;
}

/**
 * String_To_Integer - Převede string na integer
 */
int String_To_Integer(const char* str) {
    if (!str) return 0;
    return atoi(str);
}

// ============================================================================
// KONVERZNÍ FUNKCE - Real ↔ String
// ============================================================================

/**
 * Real_To_String - Převede real na string
 */
char* Real_To_String(double value) {
    static char buffer[64];
    snprintf(buffer, sizeof(buffer), "%g", value);
    return buffer;
}

/**
 * String_To_Real - Převede string na real
 */
double String_To_Real(const char* str) {
    if (!str) return 0.0;
    return atof(str);
}

// ============================================================================
// KONVERZNÍ FUNKCE - Integer ↔ Real
// ============================================================================

/**
 * Integer_To_Real - Převede integer na real
 */
double Integer_To_Real(int value) {
    return (double)value;
}

/**
 * Real_To_Integer - Převede real na integer (truncate)
 */
int Real_To_Integer(double value) {
    return (int)value;
}

// ============================================================================
// MATEMATICKÉ FUNKCE - Základní
// ============================================================================

/**
 * Sqrt - Druhá odmocnina
 */
double Sqrt(double x) {
    return sqrt(x);
}

/**
 * Abs_Integer - Absolutní hodnota pro integer
 */
int Abs_Integer(int x) {
    return abs(x);
}

/**
 * Abs_Real - Absolutní hodnota pro real
 */
double Abs_Real(double x) {
    return fabs(x);
}

/**
 * Power - Mocnina (x^y)
 */
double Power(double base, double exponent) {
    return pow(base, exponent);
}

// ============================================================================
// MATEMATICKÉ FUNKCE - Goniometrické
// ============================================================================

/**
 * Sin - Sinus
 */
double Sin(double x) {
    return sin(x);
}

/**
 * Cos - Kosinus
 */
double Cos(double x) {
    return cos(x);
}

/**
 * Tan - Tangens
 */
double Tan(double x) {
    return tan(x);
}

// ============================================================================
// MATEMATICKÉ FUNKCE - Exponenciální a logaritmické
// ============================================================================

/**
 * Exp - Exponenciální funkce (e^x)
 */
double Exp(double x) {
    return exp(x);
}

/**
 * Log - Přirozený logaritmus
 */
double Log(double x) {
    return log(x);
}

// ============================================================================
// ŘETĚZCOVÉ FUNKCE
// ============================================================================

/**
 * Length - Délka řetězce
 */
int Length(const char* str) {
    if (!str) return 0;
    return strlen(str);
}

/**
 * Substring - Vytvoří podřetězec
 * @param str Zdrojový řetězec
 * @param start Počáteční index (1-based v Ada, ale zde 0-based)
 * @param length Délka podřetězce
 */
char* Substring(const char* str, int start, int length) {
    static char buffer[MAX_STRING_LENGTH];

    if (!str || start < 0 || length < 0) {
        buffer[0] = '\0';
        return buffer;
    }

    size_t str_len = strlen(str);

    // Kontrola hranic
    if (start >= str_len) {
        buffer[0] = '\0';
        return buffer;
    }

    // Upravit délku pokud přesahuje
    if (start + length > str_len) {
        length = str_len - start;
    }

    // Zkopírovat podřetězec
    if (length >= MAX_STRING_LENGTH) {
        length = MAX_STRING_LENGTH - 1;
    }

    strncpy(buffer, str + start, length);
    buffer[length] = '\0';

    return buffer;
}

/**
 * Concat - Spojí dva řetězce
 */
char* Concat(const char* str1, const char* str2) {
    static char buffer[MAX_STRING_LENGTH];

    buffer[0] = '\0';

    if (str1) {
        strncat(buffer, str1, MAX_STRING_LENGTH - 1);
    }

    if (str2) {
        size_t current_len = strlen(buffer);
        strncat(buffer, str2, MAX_STRING_LENGTH - current_len - 1);
    }

    return buffer;
}

/**
 * To_Upper - Převede řetězec na velká písmena
 */
char* To_Upper(const char* str) {
    static char buffer[MAX_STRING_LENGTH];

    if (!str) {
        buffer[0] = '\0';
        return buffer;
    }

    size_t len = strlen(str);
    if (len >= MAX_STRING_LENGTH) {
        len = MAX_STRING_LENGTH - 1;
    }

    for (size_t i = 0; i < len; i++) {
        buffer[i] = toupper(str[i]);
    }
    buffer[len] = '\0';

    return buffer;
}

/**
 * To_Lower - Převede řetězec na malá písmena
 */
char* To_Lower(const char* str) {
    static char buffer[MAX_STRING_LENGTH];

    if (!str) {
        buffer[0] = '\0';
        return buffer;
    }

    size_t len = strlen(str);
    if (len >= MAX_STRING_LENGTH) {
        len = MAX_STRING_LENGTH - 1;
    }

    for (size_t i = 0; i < len; i++) {
        buffer[i] = tolower(str[i]);
    }
    buffer[len] = '\0';

    return buffer;
}

// ============================================================================
// FUNKCE PRO NÁHODNÁ ČÍSLA
// ============================================================================

/**
 * Random_Integer - Generuje náhodné celé číslo v rozsahu [min, max]
 */
int Random_Integer(int min, int max) {
    init_runtime();

    if (min > max) {
        int temp = min;
        min = max;
        max = temp;
    }

    return min + (rand() % (max - min + 1));
}

/**
 * Random_Real - Generuje náhodné reálné číslo v rozsahu [0.0, 1.0)
 */
double Random_Real() {
    init_runtime();
    return (double)rand() / (double)RAND_MAX;
}

// ============================================================================
// POMOCNÉ FUNKCE PRO DEBUGOVÁNÍ (volitelné)
// ============================================================================

/**
 * Debug_Print - Vypíše debug zprávu
 */
void Debug_Print(const char* msg) {
    fprintf(stderr, "[DEBUG] %s\n", msg);
}

/**
 * Debug_Print_Int - Vypíše debug zprávu s celým číslem
 */
void Debug_Print_Int(const char* msg, int value) {
    fprintf(stderr, "[DEBUG] %s: %d\n", msg, value);
}

/**
 * Debug_Print_Real - Vypíše debug zprávu s reálným číslem
 */
void Debug_Print_Real(const char* msg, double value) {
    fprintf(stderr, "[DEBUG] %s: %g\n", msg, value);
}