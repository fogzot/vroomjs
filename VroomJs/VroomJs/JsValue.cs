// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright © 2013 Federico Di Gregorio <fog@initd.org>
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
using System.Runtime.InteropServices;

namespace VroomJs
{ 
    [StructLayout(LayoutKind.Explicit)]
	struct JsValue
	{
        [FieldOffset(0)] public int    i32;
        [FieldOffset(0)] public long   i64;
        [FieldOffset(0)] public double num;
        [FieldOffset(0)] public IntPtr ptr;

        [FieldOffset(8)] public int type;
        [FieldOffset(12)] public int length;

        public const int TYPE_NULL =     0x00000000;
        public const int TYPE_OBJECT =   0x10000000;
        public const int TYPE_WRAPPED =  0x20000000;
        public const int TYPE_BOOLEAN =  0x30000000;
        public const int TYPE_INTEGER =  0x40000000;
        public const int TYPE_NUMBER =   0x50000000;
        public const int TYPE_STRING =   0x60000000;
        public const int TYPE_DATE =     0x70000000;
        public const int TYPE_ARRAY =   -2147483648; // C# doesn't like 0x80000000
        public const int TYPE_ERROR =   -1;
    }
}
