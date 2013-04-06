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
        static extern JsValue jsengine_invoke_member(HandleRef engine, IntPtr ptr, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue args);

        [DllImport("vroomjs")]
        static internal extern JsValue jsvalue_alloc_string([MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport("vroomjs")]
        static internal extern JsValue jsvalue_alloc_array(int length);

        [DllImport("vroomjs")]
        static internal extern void jsvalue_dispose(JsValue value);

        public JsEngine()
		{
            _keepalives = new Dictionary<int,object>();
            _keepalive_remove = new KeepaliveRemoveDelegate(KeepAliveRemove);
            _keepalive_get_property_value = new KeepAliveGetPropertyValueDelegate(KeepAliveGetPropertyValue);
            _keepalive_set_property_value = new KeepAliveSetPropertyValueDelegate(KeepAliveSetPropertyValue);

            _engine = new HandleRef(this, jsengine_new(_keepalive_remove, _keepalive_get_property_value, _keepalive_set_property_value));
            _convert = new JsConvert(this);
		}

        HandleRef _engine;
        JsConvert _convert;

        // Keep objects passed to V8 alive even if no other references exist.
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
            object res = _convert.FromJsValue(v);
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
            object res = _convert.FromJsValue(v);
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

            jsengine_set_variable(_engine, name, _convert.ToJsValue(value));

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
            object res = _convert.FromJsValue(v);
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

            JsValue v = jsengine_set_property_value(_engine, obj.Ptr, name, _convert.ToJsValue(value));
            object res = _convert.FromJsValue(v);
            jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
        }

        public object InvokeMember(JsObject obj, string name, object[] args)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            if (obj.Ptr == IntPtr.Zero)
                throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

            JsValue a = JsValue.Null; // Null value unless we're given args.
            if (args != null)
                a = _convert.ToJsValue(args);

            JsValue v = jsengine_invoke_member(_engine, obj.Ptr, name, a);
            object res = _convert.FromJsValue(v);
            jsvalue_dispose(v);
            jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        internal int KeepAliveSet(object obj)
        {
            _keepalives.Add(_keepalives_count, obj);
            return _keepalives_count++;
        }

        internal object KeepAliveGet(int slot)
        {
            object obj;
            if (_keepalives.TryGetValue(slot, out obj))
                return obj;
            return null;
        }

        internal void KeepAliveRemove(int slot)
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
                        return _convert.ToJsValue(pi.GetValue(obj, null));

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
                        pi.SetValue(obj, _convert.FromJsValue(value), null);
                        return JsValue.Null;
                    }

                    return JsValue.Error(KeepAliveSet(
                        new InvalidOperationException(String.Format("property not found on {0}: {1} ", type, name)))); 
                }
                catch (Exception e) {
                    return JsValue.Error(KeepAliveSet(e));
                }
            }

            return JsValue.Error(KeepAliveSet(new IndexOutOfRangeException("invalid keepalive slot: " + slot))); 
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
