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
    public class Globals
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

        [TestCase]
        public void SimpleExpressionNull()
        {
            Assert.That(js.Execute("null"), Is.Null);
        }

        [TestCase]
        public void SimpleExpressionBoolean()
        {
            Assert.That(js.Execute("0 == 0"), Is.EqualTo(true));
        }

        [TestCase]
        public void SimpleExpressionInteger()
        {
            Assert.That(js.Execute("1+1"), Is.EqualTo(2));
        }

        [TestCase]
        public void SimpleExpressionNumber()
        {
            Assert.That(js.Execute("3.14159+2.71828"), Is.EqualTo(5.85987));
        }

        [TestCase]
        public void SimpleExpressionString()
        {
            Assert.That(js.Execute("'paco'+'cico'"), Is.EqualTo("pacocico"));
        }

        [TestCase]
        public void SimpleExpressionDate()
        {
            Assert.That(js.Execute("new Date(1971, 10, 19, 0, 42, 59)") , Is.EqualTo(new DateTime(1971, 10, 19, 0, 42, 59)));
        }

        [TestCase]
        public void SimpleExpressionArray()
        {
            var res = (object[])js.Execute("['foobar', 3.14159+2.71828, 42]");
            Assert.That(res.Length, Is.EqualTo(3));
            Assert.That(res[0], Is.EqualTo("foobar"));
            Assert.That(res[1], Is.EqualTo(5.85987));
            Assert.That(res[2], Is.EqualTo(42));
        }

        [TestCase]
        public void UnicodeScript()
        {
            Assert.That(js.Execute("var àbç = 12, $ùì = 30; àbç+$ùì;"), Is.EqualTo(42));
        }

        [TestCase]
        public void SetGetVariableNull()
        {
            js.SetVariable("foo", null);
            Assert.That(js.GetVariable("foo"), Is.Null);
        }

        [TestCase]
        public void SetGetVariableBoolean()
        {
            js.SetVariable("foo", true);
            Assert.That(js.GetVariable("foo"), Is.EqualTo(true));
        }

        [TestCase]
        public void SetGetVariableInteger()
        {
            js.SetVariable("foo", 13);
            Assert.That(js.GetVariable("foo"), Is.EqualTo(13));
        }

        [TestCase]
        public void SetGetVariableNumber()
        {
            js.SetVariable("foo", 3.14159);
            Assert.That(js.GetVariable("foo"), Is.EqualTo(3.14159));
        }

        [TestCase]
        public void SetGetVariableString()
        {
            js.SetVariable("foo", "bar");
            Assert.That(js.GetVariable("foo"), Is.EqualTo("bar"));
        }

        [TestCase]
        public void SetGetVariableDate()
        {   
            var dt = new DateTime(1971, 10, 19, 0, 42, 59);
            js.SetVariable("foo", dt);
            Assert.That(js.GetVariable("foo") , Is.EqualTo(dt));
        }

        [TestCase]
        public void SetGetVariableArray()
        {
            var v = new object[] { "foobar", 3.14159, 42 };
            js.SetVariable("foo", v);
            js.Execute("foo[1] += 2.71828");
            object r = js.GetVariable("foo");
            Assert.That(r, Is.AssignableTo<object[]>());
            object[] a = (object[])r;
            Assert.That(a.Length, Is.EqualTo(3));
            Assert.That(a[0], Is.EqualTo("foobar"));
            Assert.That(a[1], Is.EqualTo(5.85987));
            Assert.That(a[2], Is.EqualTo(42));
        }
    }
}

