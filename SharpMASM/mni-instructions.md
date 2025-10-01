# MNI (Module Native Interface) Documentation

MNI functions allow you to extend MicroASM with native modules written in Java/Kotlin or C#. Here's how to use them in your assembly code.

## Basic Syntax

```
MNI ModuleName.functionName arg1 arg2 arg3...
```

## Implementing MNI Modules in Java/Kotlin

Writing a Module is easy, just open a new library project in your favorite IDE and create a new class that you want to use.

```java
public class StringOperations {
    public static void cmp(String r1, String r2) {
        // Compare strings
    }
}
```

In order to actually use this function in your assembly code, we need to use some attributes to tell the compiler that this is a MNI function.

```java
import org.finite.ModuleManager.*;
import org.finite.ModuleManager.annotations.*;

@MNIClass("StringOperations")
public class StringOperations {

    @MNIFunction(name = "cmp", module = "StringOperations")
    public static void cmp(MNIMethodObject obj)
    {
        String st1 = obj.readString(obj.arg1);
        String st2 = obj.readString(obj.arg2);

        if (st1.equals(st2))
        {
            obj.setRegister("RFLAGS",1);
        }
        else{
            obj.setRegister("RFLAGS", 0);
        }

    }
}
```

MNI method objects contain everything about what happened.
You usually have access to the registers, the arguments and the memory.
These are passed by reference so changes will affect the actual system state.

## Implementing MNI Modules in C#

Writing a Module in C# is easy. Create a class with the appropriate attributes:

```csharp
using SharpMASM.MNI;

namespace YourNamespace
{
    [MNIClass("MyModule")]
    public class MyModuleImplementation
    {
        [MNIFunction(name: "myFunction", module: "MyModule")]
        public static void MyFunction(MNIMethodObject obj)
        {
            // Read arguments
            string input = obj.readString(obj.arg1);
            
            // Process data
            string result = ProcessData(input);
            
            // Write result
            obj.writeString(obj.arg2, result);
            
            // Or set a register
            obj.setRegister("RFLAGS", 1);
        }
        
        private static string ProcessData(string input)
        {
            // Your implementation here
            return input.ToUpper();
        }
    }
}
```

MNI method objects contain everything about what happened.
You usually have access to the registers, the arguments and the memory.
These are passed by reference so changes will affect the actual system state.

Methods available on MNIMethodObject:
- `readString(location)` - Read a string from a memory location
- `readInteger(location)` - Read an integer from a memory location or register
- `writeString(location, value)` - Write a string to a memory location
- `writeInteger(location, value)` - Write an integer to a memory location or register
- `setRegister(name, value)` - Set a register value
- `getRegister(name)` - Get a register value

These method objects can be compiled into a DLL which can be put into a "modules" folder next to the interpreter. 
The interpreter will load these at runtime using reflection and allow users to use them in their programs.

## Available Modules

### StringOperations

#### String Comparison
```nasm
MNI StringOperations.cmp R1 R2    ; Compare strings at memory locations in R1 and R2
                                 ; Sets RFLAGS to 1 if equal, 0 if not
```

#### String Concatenation
```nasm
MNI StringOperations.concat R1 R2 R3    ; Concatenate strings at R1 and R2
                                       ; Result stored at memory location R3
```

#### String Length
```nasm
MNI StringOperations.length R1 R2    ; Get length of string at R1
                                    ; Result stored in register R2
```

#### String Split
```nasm
MNI StringOperations.split R1 R2 $5000    ; Split string at R1 using delimiter at R2
                                         ; Results stored starting at memory address $5000
```

#### String Replace
```nasm
MNI StringOperations.replace R1 R2 R3 R4    ; Replace in string at R1
                                           ; Replace R2 with R3
                                           ; Result stored at R4
```

### IO Operations

#### Write Output
```nasm
MNI IO.write 1 R1    ; Write string at R1 to stdout
MNI IO.write 2 R1    ; Write string at R1 to stderr
```

#### Flush Output
```nasm
MNI IO.flush 1       ; Flush stdout
MNI IO.flush 2       ; Flush stderr
```

### FileOperations

#### Read File
```nasm
; Example of reading a file
DB $1000 "example.txt"    ; Store filename at memory address $1000
MNI FileOperations.readFile R1 $2000    ; Read file named at R1
                                       ; Content stored starting at $2000
```

## Example Program

Here's a complete example that demonstrates several MNI functions:

```nasm
; Store two strings
DB $1000 "Hello, "
DB $2000 "World!"

; Concatenate strings
MOV R1 1000
MOV R2 2000
MOV R3 3000
MNI StringOperations.concat R1 R2 R3

; Print result
MOV R1 3000
MNI IO.write 1 R1
MNI IO.flush 1

; Get string length
MOV R1 3000
MNI StringOperations.len R1 R4
MOV R1 R4
OUT 1 R1    ; Print the length

HLT
```

## Notes

- Memory addresses prefixed with $ are used for data storage
- Register values are used for temporary storage and calculations
- Always ensure you have enough memory allocated for string operations
- Some operations modify the RFLAGS register which can be used for conditional jumps
