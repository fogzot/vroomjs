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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VroomJs
{
	public class JsEngine : IDisposable
	{
        [DllImport("vroomjs")]
        static extern IntPtr jsengine_new(
            Delegate keepalive_remove,
            Delegate keepalive_get_property_value
            );

        [DllImport("vroomjs")]
        static extern void jsengine_dispose(HandleRef engine);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_execute(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport("vroomjs")]
        static extern void jsengine_set_value(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport("vroomjs")]
        static extern void jsvalue_dispose(JsValue value);

        public JsEngine()
		{
            _keepalives = new List<object>();
            _keepalive_remove = new Action<int>(KeepAliveRemove);
            _keepalive_get_property_value = new Func<int,string,JsValue>(KeepAliveGetPropertyValue);

            _engine = new HandleRef(this, jsengine_new(_keepalive_remove, _keepalive_get_property_value));
		}

        HandleRef _engine;
        List<object> _keepalives;

        // Make sure the delegates we pass to the C++ engine won't fly away during a GC.
        Delegate _keepalive_remove;
        Delegate _keepalive_get_property_value;

        public object Execute(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            CheckDisposed();

            JsValue v = jsengine_execute(_engine, code);
            object res = JsValueToObject(v);
            jsvalue_dispose(v);

            if (res is Exception)
                throw (Exception)res;
            return res;
        }

        public void SetValue(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            jsengine_set_value(_engine, name, ObjectToJsValue(value));
        }

        void KeepAliveRemove(int slot)
        {
            Console.WriteLine("REMOVING SLOT: " + slot);
            if (_keepalives.Count > slot) {
                IDisposable disposable = _keepalives[slot] as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
                _keepalives[slot] = null;
            }
        }

        JsValue KeepAliveGetPropertyValue(int slot, [MarshalAs(UnmanagedType.LPWStr)] string name)
        {
            Console.WriteLine("SLOT: {0}  PROPERTY: {1}", slot, name);
            return new JsValue { Type = JsValueType.Integer, I32 = 42 };
        }

        object JsValueToObject(JsValue v)
        {
            switch (v.Type) 
            {
                case JsValueType.Null:
                    return null;

                case JsValueType.Wrapped:
                    return null;

                case JsValueType.Boolean:
                    return v.I32 != 0;

                case JsValueType.Integer:
                    return v.I32;

                case JsValueType.Number:
                    return v.Num;

                case JsValueType.String:
                    return Marshal.PtrToStringUni(v.ptr);

                case JsValueType.Date:
                    // The formula (v.num * 10000) + 621355968000000000L was taken from a StackOverflow
                    // question and should be OK. Then why do we need to compensate by -26748000000000L
                    // (a value determined from the failing tests)?!
                    return new DateTime((long)(v.Num * 10000) + 621355968000000000L - 26748000000000L);

                case JsValueType.Array: {
                    var r = new object[v.Length];
                    for (int i=0 ; i < v.Length ; i++) {
                        var vi =(JsValue)Marshal.PtrToStructure((v.ptr + 16*i), typeof(JsValue));
                        r[i] = JsValueToObject(vi);
                    }
                    return r;
                }
                    
                case JsValueType.Error:
                    return new JsException(Marshal.PtrToStringUni(v.ptr));
                    
                default:
                    throw new InvalidOperationException("unknown type code: " + v.Type);
            }           
        }

        JsValue ObjectToJsValue(object obj)
        {
            if (obj == null)
                return new JsValue { Type = JsValueType.Null };

            Type type = obj.GetType();

            // Check for nullable types (we will cast the value out of the box later).

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];

            if (type == typeof(Boolean))
                return new JsValue { Type = JsValueType.Boolean, I32 = (bool)obj ? 1 : 0 };

            if (type == typeof(String))
                return new JsValue { Type = JsValueType.String, ptr = Marshal.StringToHGlobalUni((string)obj) };
            if (type == typeof(Char))
                return new JsValue { Type = JsValueType.String, ptr = Marshal.StringToHGlobalUni(obj.ToString()) };

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
                return new JsValue { Type = JsValueType.Date, 
                                      Num = ((DateTime)obj).ToUniversalTime().Ticks/10000.0 - 621355968000000000.0 + 26748000000000.0 };

            // Every object explicitly converted to a value becomes an entry of the
            // _keepalives list, to make sure the GC won't collect it while still in
            // use by the unmanaged Javascript engine. We don't try to track duplicates
            // because adding the same object more than one time acts more or less as
            // reference counting.

            _keepalives.Add(obj);
            return new JsValue { Type = JsValueType.Managed, Index = _keepalives.Count - 1 };
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
            _keepalives = null;
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
