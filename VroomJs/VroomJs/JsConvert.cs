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
    class JsConvert
    {
        public JsConvert(JsEngine engine)
        {
            _engine = engine;
        }

        readonly JsEngine _engine;

        public object FromJsValue(JsValue v)
        {
            switch (v.Type) 
            {
                case JsValueType.Null:
                    return null;

                case JsValueType.Boolean:
                    return v.I32 != 0;

                case JsValueType.Integer:
                    return v.I32;

                case JsValueType.Number:
                    return v.Num;

                case JsValueType.String:
                    return Marshal.PtrToStringUni(v.Ptr);

                case JsValueType.Date:
                    // The formula (v.num * 10000) + 621355968000000000L was taken from a StackOverflow
                    // question and should be OK. Then why do we need to compensate by -26748000000000L
                    // (a value determined from the failing tests)?!
                    return new DateTime((long)(v.Num * 10000) + 621355968000000000L - 26748000000000L);

                case JsValueType.Array: {
                    var r = new object[v.Length];
                    for (int i=0 ; i < v.Length ; i++) {
                        var vi =(JsValue)Marshal.PtrToStructure((v.Ptr + 16*i), typeof(JsValue));
                        r[i] = FromJsValue(vi);
                    }
                    return r;
                }

                case JsValueType.UnknownError:
                    if (v.Ptr != IntPtr.Zero)
                        return new JsException(Marshal.PtrToStringUni(v.Ptr));
                    return new JsInteropException("unknown error without reason");

                case JsValueType.Error:
                    return new JsException(Marshal.PtrToStringUni(v.Ptr));

                case JsValueType.Managed:
                    return _engine.KeepAliveGet(v.Index);

                case JsValueType.ManagedError:
                    string msg = null;
                    if (v.Ptr != IntPtr.Zero)
                        msg = Marshal.PtrToStringUni(v.Ptr);
                    return new JsException(msg, _engine.KeepAliveGet(v.Index) as Exception);

                case JsValueType.Wrapped:
                    return new JsObject(_engine, v.Ptr);

                case JsValueType.WrappedError:
                return new JsException(new JsObject(_engine, v.Ptr));

                default:
                    throw new InvalidOperationException("unknown type code: " + v.Type);
            }           
        }

        public JsValue ToJsValue(object obj)
        {
            if (obj == null)
                return new JsValue { Type = JsValueType.Null };

            Type type = obj.GetType();

            // Check for nullable types (we will cast the value out of the box later).

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];

            if (type == typeof(Boolean))
                return new JsValue { Type = JsValueType.Boolean, I32 = (bool)obj ? 1 : 0 };

            if (type == typeof(String) || type == typeof(Char)) {
                // We need to allocate some memory on the other side; will be free'd by unmanaged code.
                return JsEngine.jsvalue_alloc_string(obj.ToString());
            }

            if (type == typeof(Byte))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(Byte)obj };
            if (type == typeof(Int16))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(Int16)obj };
            if (type == typeof(UInt16))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(UInt16)obj };
            if (type == typeof(Int32))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)obj };
            if (type == typeof(UInt32))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(UInt32)obj };

            if (type == typeof(Int64))
                return new JsValue { Type = JsValueType.Number, Num = (double)(Int64)obj };
            if (type == typeof(UInt64))
                return new JsValue { Type = JsValueType.Number, Num = (double)(UInt64)obj };
            if (type == typeof(Single))
                return new JsValue { Type = JsValueType.Number, Num = (double)(Single)obj };
            if (type == typeof(Double))
                return new JsValue { Type = JsValueType.Number, Num = (double)obj };
            if (type == typeof(Decimal))
                return new JsValue { Type = JsValueType.Number, Num = (double)(Decimal)obj };

            if (type == typeof(DateTime))
                return new JsValue { 
                Type = JsValueType.Date, 
                Num = (((DateTime)obj).Ticks - 621355968000000000.0 + 26748000000000.0)/10000.0 
            };

            // Arrays of anything that can be cast to object[] are recursively convertef after
            // allocating an appropriate jsvalue on the unmanaged side.

            var array = obj as object[];
            if (array != null) {
                JsValue v = JsEngine.jsvalue_alloc_array(array.Length);
                if (v.Length != array.Length)
                    throw new JsInteropException("can't allocate memory on the unmanaged side");
                for (int i=0 ; i < array.Length ; i++)
                    Marshal.StructureToPtr(ToJsValue(array[i]), (v.Ptr + 16*i), false);
                return v;
            }

            // Every object explicitly converted to a value becomes an entry of the
            // _keepalives list, to make sure the GC won't collect it while still in
            // use by the unmanaged Javascript engine. We don't try to track duplicates
            // because adding the same object more than one time acts more or less as
            // reference counting.

            return new JsValue { Type = JsValueType.Managed, Index = _engine.KeepAliveSet(obj) };
        }
    }
}
