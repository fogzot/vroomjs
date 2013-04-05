What is VroomJS
===============

VroomJS is a bridge between the .NET CLR (think C# or F#) and the V8 Javascript
engine that uses P/Invoke and a thin C layer to avoid the need to recompile V8
C++ using the MS Managed C++ compiler. That means that VroomJS is Mono-friendly
because doesn't use any feature that will make it run only on MS.NET.
