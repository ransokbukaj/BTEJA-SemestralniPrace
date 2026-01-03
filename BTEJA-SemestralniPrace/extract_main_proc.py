#!/usr/bin/env python3
"""
Ada Compiler - Main Procedure Name Extractor

Tento skript extrahuje název hlavní procedury z .ll souboru generovaného
LLVM code generatorem a vytváří správné -D definice pro kompilaci main_wrapper.c
"""

import sys
import re
import os

def extract_main_procedure_name(ll_file_path):
    """
    Extrahuje název hlavní procedury z LLVM IR (.ll) souboru.
    
    Args:
        ll_file_path: Cesta k .ll souboru
        
    Returns:
        Název hlavní procedury nebo None pokud nebyla nalezena
    """
    try:
        with open(ll_file_path, 'r') as f:
            content = f.read()
            
        # Hledáme definici funkce ve formátu: define void @NazevProcedury()
        # Ignorujeme builtin funkce (které obsahují underscore nebo začínají malým písmenem)
        pattern = r'define\s+void\s+@([A-Z][A-Za-z0-9_]*)\(\s*\)'
        matches = re.findall(pattern, content)
        
        if matches:
            # Filtrujeme builtin funkce
            builtins = ['Put_Line', 'Put', 'Put_Integer', 'Put_Real', 'New_Line', 
                       'Get_Line', 'Get', 'Get_Real']
            
            for match in matches:
                if match not in builtins and not match.startswith('_'):
                    return match
        
        return None
        
    except FileNotFoundError:
        print(f"Error: File '{ll_file_path}' not found", file=sys.stderr)
        return None
    except Exception as e:
        print(f"Error reading file: {e}", file=sys.stderr)
        return None

def generate_compile_flags(procedure_name):
    """
    Generuje kompilační flags pro main_wrapper.c
    
    Args:
        procedure_name: Název hlavní procedury
        
    Returns:
        String s -D flagem
    """
    return f"-DMAIN_PROCEDURE_NAME={procedure_name}"

def main():
    if len(sys.argv) != 2:
        print("Usage: extract_main_proc.py <file.ll>", file=sys.stderr)
        print("\nExtracts the main procedure name from LLVM IR file", file=sys.stderr)
        sys.exit(1)
    
    ll_file = sys.argv[1]
    
    if not os.path.exists(ll_file):
        print(f"Error: File '{ll_file}' does not exist", file=sys.stderr)
        sys.exit(1)
    
    procedure_name = extract_main_procedure_name(ll_file)
    
    if procedure_name:
        # Výstup pouze názvu procedury (pro použití v shellových skriptech)
        print(procedure_name)
        sys.exit(0)
    else:
        print("Error: No main procedure found in LLVM IR", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()