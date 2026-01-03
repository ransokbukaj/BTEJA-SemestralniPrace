@echo off
REM Universal Ada Compiler Build Script for Windows
REM Automatically detects main procedure name and compiles program

setlocal enabledelayedexpansion

REM Check arguments
if "%~1"=="" (
    echo Usage: %~nx0 ^<ada_source_file.ada^>
    echo.
    echo Example: %~nx0 matrix_operations.ada
    exit /b 1
)

set "ADA_SOURCE=%~1"
set "BASENAME=%~n1"
set "OUTPUT_NAME=%BASENAME%"

REM Check if source file exists
if not exist "%ADA_SOURCE%" (
    echo ERROR: Source file '%ADA_SOURCE%' not found
    exit /b 1
)

echo INFO: Compiling Ada program: %ADA_SOURCE%

REM Step 1: Check if compiler exists
set "COMPILER_EXE=AdaCompiler.exe"
if not exist "%COMPILER_EXE%" (
    echo INFO: Ada compiler not found, building it...
    dotnet build AdaCompiler.csproj -c Release
    if errorlevel 1 (
        echo ERROR: Failed to build Ada compiler
        exit /b 1
    )
    set "COMPILER_EXE=bin\Release\net8.0\AdaCompiler.exe"
)

REM Step 2: Compile .ada to .ll
echo INFO: Step 1/5: Compiling Ada source to LLVM IR...
dotnet "%COMPILER_EXE%" "%ADA_SOURCE%" "%BASENAME%.ll"
if errorlevel 1 (
    echo ERROR: Ada compilation failed
    exit /b 1
)

REM Step 3: Extract main procedure name
echo INFO: Step 2/5: Detecting main procedure name...
for /f "delims=" %%i in ('python extract_main_proc.py "%BASENAME%.ll"') do set "MAIN_PROC_NAME=%%i"
if "%MAIN_PROC_NAME%"=="" (
    echo ERROR: Could not detect main procedure name from LLVM IR
    exit /b 1
)
echo INFO: Main procedure detected: %MAIN_PROC_NAME%

REM Step 4: Convert .ll to .bc
echo INFO: Step 3/5: Converting LLVM IR to bitcode...
llvm-as "%BASENAME%.ll" -o "%BASENAME%.bc"
if errorlevel 1 (
    echo ERROR: LLVM assembly failed
    exit /b 1
)

REM Step 5: Compile runtime.c
echo INFO: Step 4/5: Compiling Ada runtime...
clang -c -emit-llvm runtime.c -o runtime.bc
if errorlevel 1 (
    echo ERROR: Runtime compilation failed
    exit /b 1
)

REM Step 6: Compile main_wrapper.c with detected procedure name
echo INFO: Step 5/5: Compiling main wrapper with procedure name: %MAIN_PROC_NAME%...
clang -c -emit-llvm -DMAIN_PROCEDURE_NAME=%MAIN_PROC_NAME% main_wrapper.c -o main_wrapper.bc
if errorlevel 1 (
    echo ERROR: Main wrapper compilation failed
    exit /b 1
)

REM Step 7: Link all .bc files
echo INFO: Linking all modules...
llvm-link "%BASENAME%.bc" runtime.bc main_wrapper.bc -o "%OUTPUT_NAME%_linked.bc"
if errorlevel 1 (
    echo ERROR: LLVM linking failed
    exit /b 1
)

REM Step 8: Generate native executable
echo INFO: Generating native executable...
clang "%OUTPUT_NAME%_linked.bc" -o "%OUTPUT_NAME%.exe" -lm
if errorlevel 1 (
    echo ERROR: Native compilation failed
    exit /b 1
)

REM Cleanup (optional - uncomment to remove intermediate files)
REM del "%BASENAME%.ll" "%BASENAME%.bc" runtime.bc main_wrapper.bc "%OUTPUT_NAME%_linked.bc"

echo =========================================
echo INFO: Compilation successful!
echo INFO: Main procedure: %MAIN_PROC_NAME%
echo INFO: Executable: %OUTPUT_NAME%.exe
echo =========================================
echo.
echo To run your program, execute:
echo %OUTPUT_NAME%.exe

endlocal