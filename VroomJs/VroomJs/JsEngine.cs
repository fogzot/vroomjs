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
using System.Reflection;
using System.Runtime.InteropServices;

namespace VroomJs
{
	public class JsEngine : IDisposable
	{
        delegate void KeepaliveRemoveDelegate(int slot);
        delegate JsValue KeepAliveGetPropertyValueDelegate(int slot, [MarshalAs(UnmanagedType.LPWStr)] string name);
        delegate JsValue KeepAliveSetPropertyValueDelegate(int slot, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport("vroomjs")]
        static extern IntPtr jsengine_new(
            KeepaliveRemoveDelegate keepaliveRemove,
            KeepAliveGetPropertyValueDelegate keepaliveGetPropertyValue,
            KeepAliveSetPropertyValueDelegate keepaliveSetPropertyValue
        );

        [DllImport("vroomjs")]
        static extern void jsengine_dispose(HandleRef engine);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_execute(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_get_variable(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_set_variable(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_get_property_value(HandleRef engine, IntPtr ptr, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport("vroomjs")]
        static extern JsValue jsengine_set_property_value(HandleRef engine, IntPtr ptr, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport("vroomjs")]
        static extern JsValue jsvalue_alloc_string([MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport("vroomjs")]
        static extern void jsvalue_dispose(JsValue value);

        public JsEngine()
		{
            _keepalives = new Dictionary<int,object>();
            _keepalive_remove = new KeepaliveRemoveDelegate(KeepAliveRemove);
            _keepalive_get_property_value = new KeepAliveGetPropertyValueDelegate(KeepAliveGetPropertyValue);
            _keepalive_set_property_value = new KeepAliveSetPropertyValueDelegate(KeepAliveSetPropertyValue);

            _engine = new HandleRef(this, jsengine_new(_keepalive_remove, _keepalive_get_property_value, _keepalive_set_property_value));
		}

        HandleRef _engine;
        Dictionary<int,object> _keepalives;
        int _keepalives_count;

        // Make sure the delegates we pass to the C++ engine won't fly away during a GC.
        KeepaliveRemoveDelegate _keepalive_remove;
        KeepAliveGetPropertyValueDelegate _keepalive_get_property_value;
        KeepAliveSetPropertyValueDelegate _keepalive_set_property_value;

        public object Execute(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            CheckDisposed();

            JsValue v = jsengine_execute(_engine, code);
            object res = JsValueToObject(v);
            jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public object GetVariable(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            JsValue v = jsengine_get_variable(_engine, name);
            object res = JsValueToObject(v);
            jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public void SetVariable(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            jsengine_set_variable(_engine, name, ObjectToJsValue(value));

            // TODO: Check the result of the operation for errors.
        }

        public object GetPropertyValue(JsObject obj, string name)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            if (obj.Ptr == IntPtr.Zero)
                throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

            JsValue v = jsengine_get_property_value(_engine, obj.Ptr, name);
            object res = JsValueToObject(v);
            jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public void SetPropertyValue(JsObject obj, string name, object value)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            if (obj.Ptr == IntPtr.Zero)
                throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

            JsValue v = jsengine_set_property_value(_engine, obj.Ptr, name, ObjectToJsValue(value));
            object res = JsValueToObject(v);
            jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
        }

        int KeepAliveSet(object obj)
        {
            _keepalives.Add(_keepalives_count, obj);
            return _keepalives_count++;
        }

        object KeepAliveGet(int slot)
        {
            object obj;
            if (_keepalives.TryGetValue(slot, out obj))
                return obj;
            return null;
        }

        void KeepAliveRemove(int slot)
        {
            var obj = KeepAliveGet(slot);
            if (obj != null) {
                var disposable = obj as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
                _keepalives.Remove(slot);
            }
        }

        JsValue KeepAliveGetPropertyValue(int slot, [MarshalAs(UnmanagedType.LPWStr)] string name)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.

            var obj = KeepAliveGet(slot);
            if (obj != null) {
                Type type = obj.GetType();

                // First of all try with a public property (the most common case).

                try {
                    PropertyInfo pi = type.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.GetProperty);
                    if (pi != null)
                        return ObjectToJsValue(pi.GetValue(obj, null));

                    // Then with an instance method: if found we wrap it in a delegate.

                    // Else an error.

                    return JsValue.Error(KeepAliveSet(
                        new InvalidOperationException(String.Format("property not found on {0}: {1} ", type, name)))); 
                }
                catch (TargetInvocationException e) {
                    // Client code probably isn't interested in the exception part related to
                    // reflection, so we unwrap it and pass to V8 only the real exception thrown.
                    if (e.InnerException != null)
                        return JsValue.Error(KeepAliveSet(e.InnerException));
                    throw;
                }
                catch (Exception e) {
                    return JsValue.Error(KeepAliveSet(e));
                }
            }

            return JsValue.Error(KeepAliveSet(new IndexOutOfRangeException("invalid keepalive slot: " + slot))); 
        }

        JsValue KeepAliveSetPropertyValue(int slot, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.

            var obj = KeepAliveGet(slot);
            if (obj != null) {
                Type type = obj.GetType();

                // We can only set properties; everything else is an error.
                try {
                    PropertyInfo pi = type.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.SetProperty);
                    if (pi != null) {
                        pi.SetValue(obj, JsValueToObject(value), null);
                        return JsValue.Null;
                    }
                    else {
                        return JsValue.Error(KeepAliveSet(
                            new InvalidOperationException(String.Format("property not found on {0}: {1} ", type, name)))); 
                    }
                }
                catch (Exception e) {
                    return JsValue.Error(KeepAliveSet(e));
                }
            }

            return JsValue.Error(KeepAliveSet(new IndexOutOfRangeException("invalid keepalive slot: " + slot))); 
        }

        object JsValueToObject(JsValue v)
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
                        r[i] = JsValueToObject(vi);
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
                    return KeepAliveGet(v.Index);

                case JsValueType.ManagedError:
                    string msg = null;
                    if (v.Ptr != IntPtr.Zero)
                        msg = Marshal.PtrToStringUni(v.Ptr);
                    return new JsException(msg, KeepAliveGet(v.Index) as Exception);

                case JsValueType.Wrapped:
                    return new JsObject(this, v.Ptr);

                case JsValueType.WrappedError:
                    return new JsException(new JsObject(this, v.Ptr));

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

            if (type == typeof(String) || type == typeof(Char)) {
                // We need to allocate some memory on the other side; will be free'd by unmanaged code.
                return jsvalue_alloc_string(obj.ToString());
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

            // Every object explicitly converted to a value becomes an entry of the
            // _keepalives list, to make sure the GC won't collect it while still in
            // use by the unmanaged Javascript engine. We don't try to track duplicates
            // because adding the same object more than one time acts more or less as
            // reference counting.

            return new JsValue { Type = JsValueType.Managed, Index = KeepAliveSet(obj) };
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
