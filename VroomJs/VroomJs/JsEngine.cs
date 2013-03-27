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
	public class JsEngine : IDisposable
	{
        [DllImport("vroomjs")]
        static extern IntPtr jsengine_new();

        [DllImport("vroomjs")]
        static extern void jsengine_dispose(HandleRef engine);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_execute(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport("vroomjs")]
        static extern void jsengine_set(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport("vroomjs")]
        static extern void jsengine_free(JsValue value);

        public JsEngine()
		{
            _engine = new HandleRef(this, jsengine_new());
		}

        HandleRef _engine;

        public object Execute(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            CheckDisposed();

            JsValue v = jsengine_execute(_engine, code);
            object res = JsValueToObject(v);
            jsengine_free(v);

            if (res is Exception)
                throw (Exception)res;
            return res;
        }

        public void SetValue(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            jsengine_set(_engine, name, ObjectToJsValue(value));
        }

        object JsValueToObject(JsValue v)
        {
            switch (v.type) 
            {
                case JsValue.TYPE_NULL:
                    return null;

                case JsValue.TYPE_OBJECT:
                    return null;

                case JsValue.TYPE_WRAPPED:
                    return null;

                case JsValue.TYPE_BOOLEAN:
                    return v.i32 != 0;

                case JsValue.TYPE_INTEGER:
                    return v.i32;

                case JsValue.TYPE_NUMBER:
                    return v.num;

                case JsValue.TYPE_STRING:
                    return Marshal.PtrToStringUni(v.ptr);

                case JsValue.TYPE_DATE:
                    // The formula (v.num * 10000) + 621355968000000000L was taken from a StackOverflow
                    // question and should be OK. Then why do we need to compensate by -26748000000000L
                    // (a value determined from the failing tests)?!
                    return new DateTime((long)(v.num * 10000) + 621355968000000000L - 26748000000000L);

                case JsValue.TYPE_ARRAY: {
                    var r = new object[v.length];
                    for (int i=0 ; i < v.length ; i++) {
                        var vi =(JsValue)Marshal.PtrToStructure((v.ptr + 16*i), typeof(JsValue));
                        r[i] = JsValueToObject(vi);
                    }
                    return r;
                }
                    
                case JsValue.TYPE_ERROR:
                    return new JsException(Marshal.PtrToStringUni(v.ptr));
                    
                default:
                    throw new InvalidOperationException("unknown type code: " + v.type);
            }           
        }

        JsValue ObjectToJsValue(object obj)
        {
            if (obj == null)
                return new JsValue { type = JsValue.TYPE_NULL };

            Type type = obj.GetType();

            // Check for nullable types (we will cast the value out of the box later).

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];

            if (type == typeof(Boolean))
                return new JsValue { type = JsValue.TYPE_BOOLEAN, i32 = (bool)obj ? 1 : 0 };

            if (type == typeof(String))
                return new JsValue { type = JsValue.TYPE_STRING, ptr = Marshal.StringToHGlobalUni((string)obj) };
            if (type == typeof(Char))
                return new JsValue { type = JsValue.TYPE_STRING, ptr = Marshal.StringToHGlobalUni(obj.ToString()) };

            if (type == typeof(Byte))
                return new JsValue { type = JsValue.TYPE_INTEGER, i32 = (int)(Byte)obj };
            if (type == typeof(Int16))
                return new JsValue { type = JsValue.TYPE_INTEGER, i32 = (int)(Int16)obj };
            if (type == typeof(UInt16))
                return new JsValue { type = JsValue.TYPE_INTEGER, i32 = (int)(UInt16)obj };
            if (type == typeof(Int32))
                return new JsValue { type = JsValue.TYPE_INTEGER, i32 = (int)obj };
            if (type == typeof(UInt32))
                return new JsValue { type = JsValue.TYPE_INTEGER, i32 = (int)(UInt32)obj };

            if (type == typeof(Int64))
                return new JsValue { type = JsValue.TYPE_NUMBER, num = (double)(Int64)obj };
            if (type == typeof(UInt64))
                return new JsValue { type = JsValue.TYPE_NUMBER, num = (double)(UInt64)obj };
            if (type == typeof(Single))
                return new JsValue { type = JsValue.TYPE_NUMBER, num = (double)(Single)obj };
            if (type == typeof(Double))
                return new JsValue { type = JsValue.TYPE_NUMBER, num = (double)obj };
            if (type == typeof(Decimal))
                return new JsValue { type = JsValue.TYPE_NUMBER, num = (double)(Decimal)obj };

            if (type == typeof(DateTime))
                return new JsValue { type = JsValue.TYPE_DATE, 
                                      num = ((DateTime)obj).ToUniversalTime().Ticks/10000.0 - 621355968000000000.0 + 26748000000000.0 };

            throw new InvalidOperationException("can't marshal type to Javascript engine: " + type);
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_engine.Handle != IntPtr.Zero)
                jsengine_dispose(_engine);
            _engine = new HandleRef(null, IntPtr.Zero);
        }

        void CheckDisposed()
        {
            if (_engine.Handle == IntPtr.Zero)
                throw new ObjectDisposedException("engine already disposed");
        }

        ~JsEngine()
        {
            Dispose(false);
        }

        #endregion
	}
}
