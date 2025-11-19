-- Příklad 3: Komplexní systém demonstrující všechny vlastnosti
-- Simulace statistického zpracování dat s vícerozměrnými poli

procedure Statistical_Analysis is
    -- Definice typů
    type Data_Array_2D is array(1..5, 1..10) of Real;
    type Integer_Grid is array(1..5, 1..10) of Integer;
    type Statistics_Array is array(1..5) of Real;
    type String_Array is array(1..5) of String;
    
    -- Globální proměnné
    data : Data_Array_2D;
    int_grid : Integer_Grid;
    row_stats : Statistics_Array;
    labels : String_Array;
    
    -- Vnořená procedura pro inicializaci dat
    procedure Initialize_Data is
        i, j : Integer;
        value : Integer;
        
        -- Hluboce vnořená funkce pro generování testovacích dat
        function Generate_Test_Value(row, col : Integer) return Integer is
            base : Integer;
            modifier : Integer;
            
            -- Další úroveň vnoření - funkce pro výpočet modifikátoru
            function Calculate_Modifier(r, c : Integer) return Integer is
                mod_val : Integer;
            begin
                mod_val := (r * c) mod 10;
                return mod_val;
            end Calculate_Modifier;
            
        begin
            base := row * 10 + col;
            modifier := Calculate_Modifier(row, col);
            return base + modifier;
        end Generate_Test_Value;
        
    begin
        Put_Line("Inicializace dat...");
        
        for i in 1..5 loop
            for j in 1..10 loop
                value := Generate_Test_Value(i, j);
                int_grid(i, j) := value;
                data(i, j) := Integer_To_Real(value);
            end loop;
        end loop;
        
        -- Inicializace popisků
        labels(1) := "Dataset_A";
        labels(2) := "Dataset_B";
        labels(3) := "Dataset_C";
        labels(4) := "Dataset_D";
        labels(5) := "Dataset_E";
    end Initialize_Data;
    
    -- Funkce pro výpočet průměru řádku s vnořenými operacemi
    function Calculate_Row_Average(arr : Data_Array_2D; row : Integer) return Real is
        sum : Real;
        count : Integer;
        j : Integer;
        avg : Real;
        
        -- Vnořená procedura pro akumulaci hodnot
        procedure Accumulate(in_out total : Real; value : Real) is
            temp : Real;
            
            -- Vnořená funkce pro filtrování extrémních hodnot
            function Filter_Value(val : Real) return Real is
                max_allowed : Real;
            begin
                max_allowed := 200.0;
                if val > max_allowed then
                    return max_allowed;
                else
                    return val;
                end if;
            end Filter_Value;
            
        begin
            temp := Filter_Value(value);
            total := total + temp;
        end Accumulate;
        
    begin
        sum := 0.0;
        count := 0;
        
        j := 1;
        loop
            Accumulate(sum, arr(row, j));
            count := count + 1;
            
            j := j + 1;
            if j > 10 then
                exit;
            end if;
        end loop;
        
        avg := sum / Integer_To_Real(count);
        return avg;
    end Calculate_Row_Average;
    
    -- Procedura pro normalizaci dat s vnořenými funkcemi
    procedure Normalize_Data(in_out arr : Data_Array_2D; averages : Statistics_Array) is
        i, j : Integer;
        
        -- Vnořená funkce pro normalizaci jedné hodnoty
        function Normalize_Value(val : Real; avg : Real; row_idx : Integer) return Real is
            normalized : Real;
            factor : Real;
            
            -- Vnořená funkce pro výpočet faktoru
            function Compute_Factor(average : Real; position : Integer) return Real is
                base_factor : Real;
                pos_real : Real;
            begin
                if average > 0.0 then
                    base_factor := 100.0 / average;
                else
                    base_factor := 1.0;
                end if;
                
                pos_real := Integer_To_Real(position);
                return base_factor * (pos_real / 5.0);
            end Compute_Factor;
            
        begin
            factor := Compute_Factor(avg, row_idx);
            normalized := val / factor;
            return normalized;
        end Normalize_Value;
        
    begin
        for i in 1..5 loop
            for j in 1..10 loop
                arr(i, j) := Normalize_Value(arr(i, j), averages(i), i);
            end loop;
        end loop;
    end Normalize_Data;
    
    -- Funkce pro nalezení maxima v řádku s rekurzivním přístupem
    function Find_Max_In_Row(arr : Data_Array_2D; row, start_col, end_col : Integer) return Real is
        mid : Integer;
        left_max, right_max : Real;
        
        -- Vnořená funkce pro porovnání dvou hodnot
        function Max_Of_Two(a, b : Real) return Real is
        begin
            if a > b then
                return a;
            else
                return b;
            end if;
        end Max_Of_Two;
        
    begin
        if start_col = end_col then
            return arr(row, start_col);
        elsif start_col + 1 = end_col then
            return Max_Of_Two(arr(row, start_col), arr(row, end_col));
        else
            mid := (start_col + end_col) / 2;
            left_max := Find_Max_In_Row(arr, row, start_col, mid);
            right_max := Find_Max_In_Row(arr, row, mid + 1, end_col);
            return Max_Of_Two(left_max, right_max);
        end if;
    end Find_Max_In_Row;
    
    -- Procedura pro výpis statistik s konverzemi
    procedure Print_Statistics(averages : Statistics_Array; labels : String_Array) is
        i : Integer;
        avg_str : String;
        
        -- Vnořená procedura pro formátovaný výpis
        procedure Print_Formatted_Line(label : String; value : Real; index : Integer) is
            value_str : String;
            index_str : String;
            
            -- Vnořená funkce pro zaokrouhlení
            function Round_Value(val : Real) return Integer is
                rounded : Integer;
            begin
                rounded := Real_To_Integer(val + 0.5);
                return rounded;
            end Round_Value;
            
            rounded : Integer;
            
        begin
            Put("[");
            Put_Integer(index);
            Put("] ");
            Put(label);
            Put(": ");
            
            rounded := Round_Value(value);
            Put_Integer(rounded);
            Put(" (exact: ");
            value_str := Real_To_String(value);
            Put(value_str);
            Put_Line(")");
        end Print_Formatted_Line;
        
    begin
        Put_Line("===== STATISTIKY =====");
        
        for i in 1..5 loop
            Print_Formatted_Line(labels(i), averages(i), i);
        end loop;
        
        New_Line;
    end Print_Statistics;
    
    -- Procedura pro zpracování celého datasetu
    procedure Process_Dataset is
        i : Integer;
        max_val : Real;
        max_str : String;
        total_avg : Real;
        sum_of_avgs : Real;
        
        -- Vnořená procedura pro zpracování jednoho řádku
        procedure Process_Row(row_num : Integer) is
            avg : Real;
            
            -- Vnořená funkce pro kontrolu validity
            function Is_Valid_Row(row : Integer) return Integer is
            begin
                if row >= 1 and row <= 5 then
                    return 1;
                else
                    return 0;
                end if;
            end Is_Valid_Row;
            
            valid : Integer;
            
        begin
            valid := Is_Valid_Row(row_num);
            
            if valid = 1 then
                avg := Calculate_Row_Average(data, row_num);
                row_stats(row_num) := avg;
                
                Put("Radek ");
                Put_Integer(row_num);
                Put(": prumer = ");
                Put(Real_To_String(avg));
                New_Line;
            else
                Put_Line("Neplatny radek!");
            end if;
        end Process_Row;
        
    begin
        Put_Line("Zpracovani datasetu:");
        
        -- Zpracování všech řádků s for cyklem
        for i in 1..5 loop
            Process_Row(i);
        end loop;
        
        New_Line;
        
        -- Výpočet celkového průměru
        sum_of_avgs := 0.0;
        i := 1;
        loop
            sum_of_avgs := sum_of_avgs + row_stats(i);
            i := i + 1;
            if i > 5 then
                exit;
            end if;
        end loop;
        
        total_avg := sum_of_avgs / 5.0;
        Put("Celkovy prumer: ");
        Put_Line(Real_To_String(total_avg));
        New_Line;
        
        -- Nalezení maxim v každém řádku
        Put_Line("Maximalni hodnoty v radcich:");
        for i in 1..5 loop
            max_val := Find_Max_In_Row(data, i, 1, 10);
            max_str := Real_To_String(max_val);
            Put("Radek ");
            Put_Integer(i);
            Put(": max = ");
            Put_Line(max_str);
        end loop;
        
        New_Line;
    end Process_Dataset;
    
    -- Test všech typů konverzí
    procedure Test_All_Conversions is
        int_val : Integer;
        real_val : Real;
        str_val : String;
        
        -- Vnořená procedura pro test konverzního řetězce
        procedure Test_Conversion_Chain is
            i1, i2 : Integer;
            r1, r2 : Real;
            s1 : String;
            
        begin
            Put_Line("Test konverzniho retezce:");
            
            i1 := 42;
            Put("Integer: ");
            Put_Integer(i1);
            New_Line;
            
            s1 := Integer_To_String(i1);
            Put("-> String: ");
            Put_Line(s1);
            
            i2 := String_To_Integer(s1);
            Put("-> Integer: ");
            Put_Integer(i2);
            New_Line;
            
            r1 := Integer_To_Real(i2);
            Put("-> Real: ");
            Put_Line(Real_To_String(r1));
            
            i1 := Real_To_Integer(r1);
            Put("-> Integer: ");
            Put_Integer(i1);
            New_Line;
            
            New_Line;
        end Test_Conversion_Chain;
        
    begin
        Put_Line("===== TEST KONVERZI =====");
        
        int_val := 123;
        str_val := Integer_To_String(int_val);
        Put("Integer ");
        Put_Integer(int_val);
        Put(" -> String: ");
        Put_Line(str_val);
        
        real_val := 45.67;
        str_val := Real_To_String(real_val);
        Put("Real ");
        Put_Real(real_val);
        Put(" -> String: ");
        Put_Line(str_val);
        
        real_val := Integer_To_Real(int_val);
        Put("Integer ");
        Put_Integer(int_val);
        Put(" -> Real: ");
        Put_Real(real_val);
        New_Line;
        
        int_val := Real_To_Integer(real_val);
        Put("Real ");
        Put_Real(real_val);
        Put(" -> Integer: ");
        Put_Integer(int_val);
        New_Line;
        
        New_Line;
        
        Test_Conversion_Chain;
    end Test_All_Conversions;
    
    -- Hlavní tělo programu
    test_choice : Integer;
    
begin
    Put_Line("========================================");
    Put_Line("  KOMPLEXNI STATISTICKA ANALYZA");
    Put_Line("========================================");
    New_Line;
    
    -- Inicializace
    Initialize_Data;
    New_Line;
    
    -- Test konverzí
    Test_All_Conversions;
    
    -- Zpracování datasetu
    Process_Dataset;
    
    -- Výpis statistik
    Print_Statistics(row_stats, labels);
    
    -- Normalizace dat
    Put_Line("Normalizace dat...");
    Normalize_Data(data, row_stats);
    Put_Line("Data normalizovana");
    New_Line;
    
    -- Zpracování po normalizaci
    Put_Line("Statistiky po normalizaci:");
    for test_choice in 1..5 loop
        row_stats(test_choice) := Calculate_Row_Average(data, test_choice);
    end loop;
    Print_Statistics(row_stats, labels);
    
    -- Test všech řídících struktur
    Put_Line("===== TEST RIDICICH STRUKTUR =====");
    
    test_choice := 3;
    
    -- Test if-elsif-else
    if test_choice < 2 then
        Put_Line("Volba < 2");
    elsif test_choice < 4 then
        Put_Line("Volba mezi 2 a 3");
    else
        Put_Line("Volba >= 4");
    end if;
    
    -- Test loop s exit
    Put("Loop test: ");
    test_choice := 1;
    loop
        Put_Integer(test_choice);
        Put(" ");
        test_choice := test_choice + 1;
        if test_choice > 5 then
            exit;
        end if;
    end loop;
    New_Line;
    
    -- Test for s reverse
    Put("For reverse test: ");
    for test_choice in reverse 1..5 loop
        Put_Integer(test_choice);
        Put(" ");
    end loop;
    New_Line;
    
    Put_Line("========================================");
    Put_Line("  ANALYZA DOKONCENA");
    Put_Line("========================================");
end Statistical_Analysis;