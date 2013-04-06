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
using VroomJs;

namespace Sandbox
{
    class Sandbox
    {
        public static void Main (string[] args)
        {
            using (JsEngine js = new JsEngine()) {
                for (int i=0 ; i < 10 ; i++) {
                    js.SetVariable("a", new Simple { N = i, S = (i*10).ToString() });
                    Console.WriteLine(js.Execute("a.N+' '+a.S"));
                }
                Console.WriteLine(js.Execute("a.N+' '+a.X"));
            }
        }
    }

    class Simple 
    {
        public int N { get; set; }
        public string S { get; set; }
        public string X {
            get { throw new InvalidOperationException("ahr ahr"); }
        }
    }
}
