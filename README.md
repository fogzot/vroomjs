What is VroomJS
===============

VroomJS is a bridge between the .NET CLR (think C# or F#) and the V8 Javascript
engine that uses P/Invoke and a thin C layer to avoid the need to recompile V8
C++ using the MS Managed C++ compiler. That means that VroomJS is Mono-friendly
because doesn't use any feature that will make it run only on MS.NET.

With VroomJs it is possible to execute arbitrary javascript code and get the
result as a managed primitive type (for integers, numbers, strings, dates and
arrays of primitive types) or as a `JsObject` wrapper that allows to
dynamically access properties and call functions on Javascript objects.

Each `JsEngine` is an isolated V8 context and all objects allocated on the
Javascript side are persistent over multiple calls. It is possible to set and
get global variables. Variable values can be primitive types, CLR objects or
`JsObjects` wrapping Javascript objects. CLR instances are kept alive as long
as used in Javascript code (so it isn't required to track them in client code:
they won't be garbage collected as long as references on the V8 side) and it is
possible to access their properties and call methods from JS code.

Examples
--------

Execute some Javascript:

	using (var js = new JsEngine()) {
		var x = (int)js.Execute("3.14159+2.71828");
		Console.WriteLine(x);  // prints 5.85987
	}

Create and return a Javascript object, then call a method on it:

	using (var js = new JsEngine()) {
		// Create a global variable on the JS side.
		js.Execute("var x = {'answer':42, 'tellme':function (x) { return x+' '+this.answer; }}");
		// Get it and use "dynamic" to tell the compiler to use runtime binding.
		dynamic x = js.GetVariable("x");
		// Call the method and print the result. This will print:
		// "What is the answer to ...? The answer is: 42"
		Console.WriteLine(x.tellme("What is the answer to ...?"));
	}
