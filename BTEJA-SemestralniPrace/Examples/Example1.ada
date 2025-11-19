-- Příklad 1: Maticové operace s různými datovými typy a konverzemi
-- Demonstruje: 2D pole, konverze typů, vnořené funkce, všechny řídící struktury

procedure Matrix_Operations is
    -- Definice typů pro 2D pole
    type Integer_Matrix is array(1..3, 1..3) of Integer;
    type Real_Matrix is array(1..3, 1..3) of Real;
    
    -- Globální matice
    int_matrix : Integer_Matrix;
    real_matrix : Real_Matrix;
    result_matrix : Real_Matrix;
    
    -- Funkce pro vytvoření identity matice (Integer)
    function Create_Identity_Integer return Integer_Matrix is
        identity : Integer_Matrix;
        i, j : Integer;
    begin
        for i in 1..3 loop
            for j in 1..3 loop
                if i = j then
                    identity(i, j) := 1;
                else
                    identity(i, j) := 0;
                end if;
            end loop;
        end loop;
        return identity;
    end Create_Identity_Integer;
    
    -- Funkce pro konverzi Integer matice na Real matici
    function Integer_To_Real_Matrix(int_mat : Integer_Matrix) return Real_Matrix is
        real_mat : Real_Matrix;
        i, j : Integer;
        temp_int : Integer;
        temp_real : Real;
    begin
        for i in 1..3 loop
            for j in 1..3 loop
                temp_int := int_mat(i, j);
                temp_real := Integer_To_Real(temp_int);
                real_mat(i, j) := temp_real;
            end loop;
        end loop;
        return real_mat;
    end Integer_To_Real_Matrix;
    
    -- Procedura pro násobení matice skalárem s vnořenou funkcí
    procedure Multiply_By_Scalar(mat : in out Real_Matrix; scalar : Real) is
        i, j : Integer;
        
        -- Vnořená funkce pro výpočet násobku prvku
        function Compute_Element(element : Real; multiplier : Real) return Real is
            result : Real;
        begin
            result := element * multiplier;
            return result;
        end Compute_Element;
        
    begin
        for i in 1..3 loop
            for j in 1..3 loop
                mat(i, j) := Compute_Element(mat(i, j), scalar);
            end loop;
        end loop;
    end Multiply_By_Scalar;
    
    -- Funkce pro sčítání dvou matic s vnořenou procedurou pro validaci
    function Add_Matrices(mat1, mat2 : Real_Matrix) return Real_Matrix is
        result : Real_Matrix;
        i, j : Integer;
        
        -- Vnořená procedura pro kontrolu a sčítání prvku
        procedure Add_And_Validate(a, b : Real; out sum : Real) is
        begin
            sum := a + b;
            -- Kontrola přetečení (simulace)
            if sum > 1000.0 then
                Put_Line("Warning: Large value detected");
            end if;
        end Add_And_Validate;
        
    begin
        for i in 1..3 loop
            for j in 1..3 loop
                Add_And_Validate(mat1(i, j), mat2(i, j), result(i, j));
            end loop;
        end loop;
        return result;
    end Add_Matrices;
    
    -- Procedura pro tisk matice se stringovými konverzemi
    procedure Print_Matrix(mat : Real_Matrix; name : String) is
        i, j : Integer;
        value_str : String;
    begin
        Put_Line(name);
        Put_Line("-------------------");
        
        i := 1;
        loop
            j := 1;
            loop
                value_str := Real_To_String(mat(i, j));
                Put(value_str);
                Put(" ");
                
                j := j + 1;
                if j > 3 then
                    exit;
                end if;
            end loop;
            New_Line;
            
            i := i + 1;
            if i > 3 then
                exit;
            end if;
        end loop;
        New_Line;
    end Print_Matrix;
    
    -- Funkce pro výpočet průměru prvků matice
    function Calculate_Average(mat : Real_Matrix) return Real is
        sum : Real;
        count : Integer;
        i, j : Integer;
        average : Real;
    begin
        sum := 0.0;
        count := 0;
        
        for i in 1..3 loop
            for j in 1..3 loop
                sum := sum + mat(i, j);
                count := count + 1;
            end loop;
        end loop;
        
        average := sum / Integer_To_Real(count);
        return average;
    end Calculate_Average;
    
    -- Hlavní tělo programu
    i, j : Integer;
    scalar : Real;
    avg : Real;
    avg_str : String;
    
begin
    Put_Line("=== Maticove operace s konverzemi typu ===");
    New_Line;
    
    -- Vytvoření identity matice (Integer)
    Put_Line("Krok 1: Vytvoreni identity matice (Integer)");
    int_matrix := Create_Identity_Integer;
    
    -- Konverze na Real matici
    Put_Line("Krok 2: Konverze na Real matici");
    real_matrix := Integer_To_Real_Matrix(int_matrix);
    Print_Matrix(real_matrix, "Identity Matrix (Real):");
    
    -- Inicializace testovací matice ručně
    Put_Line("Krok 3: Vytvoreni testovaci matice");
    for i in 1..3 loop
        for j in 1..3 loop
            real_matrix(i, j) := Integer_To_Real(i * 3 + j);
        end loop;
    end loop;
    Print_Matrix(real_matrix, "Test Matrix:");
    
    -- Násobení skalárem
    Put_Line("Krok 4: Nasobeni matice skalarem 2.5");
    scalar := 2.5;
    Multiply_By_Scalar(real_matrix, scalar);
    Print_Matrix(real_matrix, "Matrix * 2.5:");
    
    -- Výpočet průměru
    Put_Line("Krok 5: Vypocet prumeru prvku");
    avg := Calculate_Average(real_matrix);
    avg_str := Real_To_String(avg);
    Put("Prumer: ");
    Put_Line(avg_str);
    New_Line;
    
    -- Sčítání matic
    Put_Line("Krok 6: Scitani matic");
    result_matrix := Integer_To_Real_Matrix(Create_Identity_Integer);
    result_matrix := Add_Matrices(real_matrix, result_matrix);
    Print_Matrix(result_matrix, "Result of Addition:");
    
    -- Test podmínek s různými typy
    Put_Line("Krok 7: Testovani podminek");
    if avg > 10.0 then
        Put_Line("Prumer je vetsi nez 10");
    elsif avg > 5.0 then
        Put_Line("Prumer je mezi 5 a 10");
    else
        Put_Line("Prumer je mensi nez 5");
    end if;
    
    Put_Line("=== Konec programu ===");
end Matrix_Operations;