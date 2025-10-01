# TemplateGame

This is a minimal template for creating a new Asmo demo/game.

## How to use
- Copy this folder and rename it for your new demo/game.
- Edit `Game.cs` to implement your game logic.
- Reference your new game in your launcher or test harness as needed.

## Features
- Implements `IConsoleGame`
- Handles basic input and drawing
- Simple player movement example

---
Happy coding!

---

# Python Turtle Demo for Asmo

This demo shows how to control a C# turtle using Python code via Python.NET.

## How it works
- The C# `TurtleController` class exposes movement and drawing methods.
- A Python script is run at startup using Python.NET, calling methods on the C# turtle.
- The turtle's path is drawn on the Asmo display.

## Requirements
- .NET 9.0+
- Python 3.x installed and available in PATH
- The `pythonnet` NuGet package (already referenced)

## Running the Demo
From the root of the Asmo repo:

```powershell
# Build the solution (if not already built)
dotnet build

# Run the AsmoRuntime (or your launcher that loads the turtle demo)
dotnet run --project .\AsmoRuntime\
```

The turtle will draw a square, controlled by Python code.

---
You can edit the Python script in `Game.cs` to try different turtle patterns!
