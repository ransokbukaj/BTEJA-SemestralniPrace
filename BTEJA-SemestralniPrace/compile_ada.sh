#!/bin/bash
# Universal Ada Compiler Build Script
# Automaticky detekuje název hlavní procedury a kompiluje program

set -e  # Exit on error

# Barvy pro výstup
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Funkce pro výpis chybových zpráv
error() {
    echo -e "${RED}ERROR: $1${NC}" >&2
    exit 1
}

# Funkce pro výpis info zpráv
info() {
    echo -e "${GREEN}INFO: $1${NC}"
}

# Funkce pro výpis varování
warn() {
    echo -e "${YELLOW}WARNING: $1${NC}"
}

# Kontrola argumentů
if [ $# -ne 1 ]; then
    echo "Usage: $0 <ada_source_file.ada>"
    echo ""
    echo "Example: $0 matrix_operations.ada"
    exit 1
fi

ADA_SOURCE="$1"
BASENAME=$(basename "$ADA_SOURCE" .ada)
OUTPUT_NAME="$BASENAME"

# Kontrola existence vstupního souboru
if [ ! -f "$ADA_SOURCE" ]; then
    error "Source file '$ADA_SOURCE' not found"
fi

info "Compiling Ada program: $ADA_SOURCE"

# Krok 1: Kompilace Ada kompilátoru (pokud ještě není zkompilovaný)
COMPILER_EXE="AdaCompiler.exe"
if [ ! -f "$COMPILER_EXE" ]; then
    info "Ada compiler not found, building it..."
    dotnet build AdaCompiler.csproj -c Release || error "Failed to build Ada compiler"
    COMPILER_EXE="bin/Release/net8.0/AdaCompiler.exe"
fi

# Krok 2: Kompilace .ada -> .ll pomocí Ada kompilátoru
info "Step 1/5: Compiling Ada source to LLVM IR..."
dotnet "$COMPILER_EXE" "$ADA_SOURCE" "${BASENAME}.ll" || error "Ada compilation failed"

# Krok 3: Extrakce názvu hlavní procedury z .ll souboru
info "Step 2/5: Detecting main procedure name..."
MAIN_PROC_NAME=$(python3 extract_main_proc.py "${BASENAME}.ll")
if [ -z "$MAIN_PROC_NAME" ]; then
    error "Could not detect main procedure name from LLVM IR"
fi
info "Main procedure detected: $MAIN_PROC_NAME"

# Krok 4: Kompilace .ll -> .bc (LLVM bitcode)
info "Step 3/5: Converting LLVM IR to bitcode..."
llvm-as "${BASENAME}.ll" -o "${BASENAME}.bc" || error "LLVM assembly failed"

# Krok 5: Kompilace runtime.c
info "Step 4/5: Compiling Ada runtime..."
clang -c -emit-llvm runtime.c -o runtime.bc || error "Runtime compilation failed"

# Krok 6: Kompilace main_wrapper.c s automaticky detekovaným názvem procedury
info "Step 5/5: Compiling main wrapper with procedure name: $MAIN_PROC_NAME..."
clang -c -emit-llvm -DMAIN_PROCEDURE_NAME="$MAIN_PROC_NAME" main_wrapper.c -o main_wrapper.bc || error "Main wrapper compilation failed"

# Krok 7: Linkování všech .bc souborů dohromady
info "Linking all modules..."
llvm-link "${BASENAME}.bc" runtime.bc main_wrapper.bc -o "${OUTPUT_NAME}_linked.bc" || error "LLVM linking failed"

# Krok 8: Kompilace do nativního kódu
info "Generating native executable..."
clang "${OUTPUT_NAME}_linked.bc" -o "$OUTPUT_NAME" -lm || error "Native compilation failed"

# Úklid (volitelné - odkomentovat pokud chcete mazat mezivýsledky)
# rm -f "${BASENAME}.ll" "${BASENAME}.bc" runtime.bc main_wrapper.bc "${OUTPUT_NAME}_linked.bc"

info "========================================="
info "Compilation successful!"
info "Main procedure: $MAIN_PROC_NAME"
info "Executable: ./$OUTPUT_NAME"
info "========================================="
info ""
info "To run your program, execute:"
echo -e "${GREEN}./$OUTPUT_NAME${NC}"