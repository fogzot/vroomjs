// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright (c) 2013 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using NUnit.Framework;

namespace VroomJs.Tests
{
    [TestFixture]
    public class Objects
    {
        JsEngine js;

        [SetUp]
        public void Setup()
        {
            js = new JsEngine();
        }

        [TearDown]
        public void Teardown()
        {
            js.Dispose();
        }

        [Test]
        public void GetManagedIntegerProperty()
        {
            var v = 42;
            var t = new TestClass { Int32Property = v };
            js.SetVariable("o", t);
            Assert.That(js.Execute("o.Int32Property"), Is.EqualTo(v));
        }

        [Test]
        public void GetManagedStringProperty()
        {
            var v = "The lazy dog bla bla bla...";
            var t = new TestClass { StringProperty = v };
            js.SetVariable("o", t);
            Assert.That(js.Execute("o.StringProperty"), Is.EqualTo(v));
        }

        [Test]
        public void GetManagedNestedProperty()
        {
            var v = "The lazy dog bla bla bla...";
            var t = new TestClass { NestedObject = new TestClass { StringProperty = v }};
            js.SetVariable("o", t);
            Assert.That(js.Execute("o.NestedObject.StringProperty"), Is.EqualTo(v));
        }

        [Test]
        public void SetManagedIntegerProperty()
        {
            var t = new TestClass();
            js.SetVariable("o", t);
            js.Execute("o.Int32Property = 42");
            Assert.That(t.Int32Property, Is.EqualTo(42));
        }

        [Test]
        public void SetManagedStringProperty()
        {
            var t = new TestClass();
            js.SetVariable("o", t);
            js.Execute("o.StringProperty = 'This was set from Javascript!'");
            Assert.That(t.StringProperty, Is.EqualTo("This was set from Javascript!"));
        }

        [Test]
        public void SetManagedNestedProperty()
        {
            var t = new TestClass();
            var n = new TestClass();
            js.SetVariable("o", t);
            js.SetVariable("n", n);
            js.Execute("o.NestedObject = n; o.NestedObject.Int32Property = 42");
            Assert.That(t.NestedObject, Is.EqualTo(n));
            Assert.That(n.Int32Property, Is.EqualTo(42));
        }

        [Test]
        public void GetJsIntegerProperty()
        {
            js.Execute("var x = { the_answer: 42 }");
            dynamic x = js.GetVariable("x");
            Assert.That(x.the_answer, Is.EqualTo(42));
        }

        [Test]
        public void GetJsStringProperty()
        {
            js.Execute("var x = { a_string: 'This was set from Javascript!' }");
            dynamic x = js.GetVariable("x");
            Assert.That(x.a_string, Is.EqualTo("This was set from Javascript!"));
        }

        [Test]
        public void GetMixedProperty1()
        {
            var t = new TestClass();
            js.SetVariable("o", t);
            js.Execute("var x = { nested: o }; x.nested.Int32Property = 42");
            dynamic x = js.GetVariable("x");
            Assert.That(x.nested, Is.EqualTo(t));
            Assert.That(t.Int32Property, Is.EqualTo(42));
        }

        [Test]
        public void SetJsIntegerProperty()
        {
            var v = 42;
            js.Execute("var x = {}");
            dynamic x = js.GetVariable("x");
            x.the_answer = v;
            Assert.That(js.Execute("x.the_answer"), Is.EqualTo(v));
        }

        [Test]
        public void SetJsStringProperty()
        {
            var v = "This was set from managed code!";
            js.Execute("var x = {}");
            dynamic x = js.GetVariable("x");
            x.a_string = v;
            Assert.That(js.Execute("x.a_string"), Is.EqualTo(v));
        }

    }
}

