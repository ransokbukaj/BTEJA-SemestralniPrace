-- Příklad 2: Rekurzivní operace s 3D polem a demonstrace zásobníkových rámců
-- Demonstruje: 3D pole, rekurze, vnořené procedury/funkce, all control structures

procedure Recursive_Array_Operations is
    -- Definice 3D pole pro reprezentaci krychle
    type Cube_Array is array(1..4, 1..4, 1..4) of Integer;
    type Real_Cube is array(1..4, 1..4, 1..4) of Real;
    
    cube : Cube_Array;
    real_cube : Real_Cube;
    
    -- Rekurzivní funkce pro výpočet faktoriálu (demonstrace zásobníkových rámců)
    function Factorial(n : Integer) return Integer is
        result : Integer;
        
        -- Vnořená funkce pro výpočet mocniny (další úroveň vnoření)
        function Power_Of_Two(exp : Integer) return Integer is
        begin
            if exp <= 0 then
                return 1;
            else
                return 2 * Power_Of_Two(exp - 1);
            end if;
        end Power_Of_Two;
        
    begin
        if n <= 1 then
            return 1;
        else
            result := n * Factorial(n - 1);
            return result;
        end if;
    end Factorial;
    
    -- Rekurzivní procedura pro vyplnění 3D pole
    procedure Fill_Cube_Recursive(arr : in out Cube_Array; 
                                   x, y, z : Integer; 
                                   value : Integer) is
        next_value : Integer;
        
        -- Vnořená procedura pro validaci indexů
        procedure Validate_Indices(ix, iy, iz : Integer; out valid : Integer) is
        begin
            if ix >= 1 and ix <= 4 and iy >= 1 and iy <= 4 and iz >= 1 and iz <= 4 then
                valid := 1;
            else
                valid := 0;
            end if;
        end Validate_Indices;
        
        is_valid : Integer;
        
    begin
        Validate_Indices(x, y, z, is_valid);
        
        if is_valid = 0 then
            return;
        end if;
        
        arr(x, y, z) := value;
        
        -- Rekurzivně zpracovat další pozice
        if z < 4 then
            next_value := value + 1;
            Fill_Cube_Recursive(arr, x, y, z + 1, next_value);
        elsif y < 4 then
            next_value := value + 1;
            Fill_Cube_Recursive(arr, x, y + 1, 1, next_value);
        elsif x < 4 then
            next_value := value + 1;
            Fill_Cube_Recursive(arr, x + 1, 1, 1, next_value);
        end if;
    end Fill_Cube_Recursive;
    
    -- Funkce pro převod celé krychle na Real
    function Convert_Cube_To_Real(int_cube : Cube_Array) return Real_Cube is
        result : Real_Cube;
        x, y, z : Integer;
    begin
        for x in 1..4 loop
            for y in 1..4 loop
                for z in 1..4 loop
                    result(x, y, z) := Integer_To_Real(int_cube(x, y, z));
                end loop;
            end loop;
        end loop;
        return result;
    end Convert_Cube_To_Real;
    
    -- Rekurzivní funkce pro výpočet sumy v daném rozsahu
    function Sum_Range_Recursive(arr : Cube_Array; 
                                  x1, y1, z1 : Integer;
                                  x2, y2, z2 : Integer) return Integer is
        sum : Integer;
        
        -- Vnořená rekurzivní funkce pro sčítání v jedné rovině
        function Sum_Plane(arr : Cube_Array; x, y1, z1, y2, z2 : Integer) return Integer is
            plane_sum : Integer;
        begin
            if y1 > y2 then
                return 0;
            end if;
            
            if z1 > z2 then
                return Sum_Plane(arr, x, y1 + 1, 1, y2, z2);
            end if;
            
            plane_sum := arr(x, y1, z1);
            plane_sum := plane_sum + Sum_Plane(arr, x, y1, z1 + 1, y2, z2);
            return plane_sum;
        end Sum_Plane;
        
    begin
        if x1 > x2 then
            return 0;
        end if;
        
        sum := Sum_Plane(arr, x1, y1, z1, y2, z2);
        sum := sum + Sum_Range_Recursive(arr, x1 + 1, y1, z1, x2, y2, z2);
        return sum;
    end Sum_Range_Recursive;
    
    -- Procedura pro aplikaci funkce na každý prvek krychle
    procedure Transform_Cube(arr : in out Real_Cube) is
        x, y, z : Integer;
        
        -- Vnořená funkce pro transformaci hodnoty
        function Transform_Value(val : Real; x_pos, y_pos, z_pos : Integer) return Real is
            factor : Real;
            result : Real;
            
            -- Další úroveň vnořené funkce
            function Calculate_Factor(a, b, c : Integer) return Real is
                sum : Integer;
            begin
                sum := a + b + c;
                return Integer_To_Real(sum) / 10.0;
            end Calculate_Factor;
            
        begin
            factor := Calculate_Factor(x_pos, y_pos, z_pos);
            result := val * factor;
            return result;
        end Transform_Value;
        
    begin
        x := 1;
        loop
            y := 1;
            loop
                z := 1;
                loop
                    arr(x, y, z) := Transform_Value(arr(x, y, z), x, y, z);
                    
                    z := z + 1;
                    if z > 4 then
                        exit;
                    end if;
                end loop;
                
                y := y + 1;
                if y > 4 then
                    exit;
                end if;
            end loop;
            
            x := x + 1;
            if x > 4 then
                exit;
            end if;
        end loop;
    end Transform_Cube;
    
    -- Procedura pro tisk vrstvy krychle
    procedure Print_Layer(arr : Real_Cube; layer : Integer) is
        y, z : Integer;
        val_str : String;
    begin
        Put("Layer ");
        Put_Integer(layer);
        Put_Line(":");
        
        for y in 1..4 loop
            for z in 1..4 loop
                val_str := Real_To_String(arr(layer, y, z));
                Put(val_str);
                Put(" ");
            end loop;
            New_Line;
        end loop;
        New_Line;
    end Print_Layer;
    
    -- Hlavní tělo programu
    i : Integer;
    total_sum : Integer;
    sum_str : String;
    fact_5 : Integer;
    layer_num : Integer;
    
begin
    Put_Line("=== Rekurzivni operace s 3D polem ===");
    New_Line;
    
    -- Test faktoriálu (demonstrace rekurze)
    Put_Line("Krok 1: Vypocet faktorialu 5");
    fact_5 := Factorial(5);
    Put("5! = ");
    Put_Integer(fact_5);
    New_Line;
    New_Line;
    
    -- Vyplnění krychle rekurzivně
    Put_Line("Krok 2: Rekurzivni vyplneni krychle");
    Fill_Cube_Recursive(cube, 1, 1, 1, 1);
    Put_Line("Krychle vyplnena");
    New_Line;
    
    -- Výpočet sumy v rozsahu
    Put_Line("Krok 3: Vypocet sumy v rozsahu [1,1,1] - [2,2,2]");
    total_sum := Sum_Range_Recursive(cube, 1, 1, 1, 2, 2, 2);
    Put("Suma: ");
    Put_Integer(total_sum);
    New_Line;
    New_Line;
    
    -- Konverze na Real krychli
    Put_Line("Krok 4: Konverze na Real krychli");
    real_cube := Convert_Cube_To_Real(cube);
    Put_Line("Konverze dokoncena");
    New_Line;
    
    -- Aplikace transformace
    Put_Line("Krok 5: Aplikace transformace na kazdy prvek");
    Transform_Cube(real_cube);
    Put_Line("Transformace dokoncena");
    New_Line;
    
    -- Tisk několika vrstev
    Put_Line("Krok 6: Tisk vrstev krychle");
    for layer_num in 1..2 loop
        Print_Layer(real_cube, layer_num);
    end loop;
    
    -- Test všech řídících struktur
    Put_Line("Krok 7: Test ridicich struktur");
    
    i := 1;
    loop
        if i > 3 then
            exit;
        elsif i = 2 then
            Put_Line("Iterace 2 (elsif)");
        else
            Put("Iterace ");
            Put_Integer(i);
            New_Line;
        end if;
        i := i + 1;
    end loop;
    
    Put_Line("=== Konec programu ===");
end Recursive_Array_Operations;