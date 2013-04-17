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

#include "vroomjs.h"

using namespace v8;

extern "C" jsvalue jsvalue_alloc_array(const int32_t length);

static Handle<Value> managed_prop_get(Local<String> name, const AccessorInfo& info)
{
    HandleScope scope;
    
    Local<Object> self = info.Holder();
    Local<External> wrap = Local<External>::Cast(self->GetInternalField(0));
    ManagedRef* ref = (ManagedRef*)wrap->Value();
    return scope.Close(ref->GetPropertyValue(name));
}

static Handle<Value> managed_prop_set(Local<String> name, Local<Value> value, const AccessorInfo& info)
{
    HandleScope scope;
    
    Local<Object> self = info.Holder();
    Local<External> wrap = Local<External>::Cast(self->GetInternalField(0));
    ManagedRef* ref = (ManagedRef*)wrap->Value();
    return scope.Close(ref->SetPropertyValue(name, value));
}

static Handle<Value> managed_call(const Arguments& args)
{
    HandleScope scope;
    
    Local<Object> self = args.Holder();
    Local<External> wrap = Local<External>::Cast(self->GetInternalField(0));
    ManagedRef* ref = (ManagedRef*)wrap->Value();
    return scope.Close(ref->Invoke(args));
}

static void managed_destroy(Persistent<Value> object, void* parameter)
{
    HandleScope scope;
    
    Persistent<Object> self = Persistent<Object>::Cast(object);
    Local<External> wrap = Local<External>::Cast(self->GetInternalField(0));
    delete (ManagedRef*)wrap->Value();
    object.Dispose();
}

JsEngine* JsEngine::New()
{
    JsEngine* engine = new JsEngine();
    if (engine != NULL) {            
        engine->isolate_ = Isolate::New();
        Locker locker(engine->isolate_);
        Isolate::Scope isolate_scope(engine->isolate_);
        engine->context_ = new Persistent<Context>(Context::New());
        
        (*(engine->context_))->Enter();
        
        // Setup the template we'll use for all managed object references.
        HandleScope scope;            
        Handle<ObjectTemplate> o = ObjectTemplate::New();
        o->SetInternalFieldCount(1);
        o->SetNamedPropertyHandler(managed_prop_get, managed_prop_set);
        o->SetCallAsFunctionHandler(managed_call);
        Persistent<ObjectTemplate> p = Persistent<ObjectTemplate>::New(o);
        engine->managed_template_ = new Persistent<ObjectTemplate>(p);
    }
    
    return engine;
}

void JsEngine::Dispose()
{
    {
        Locker locker(isolate_);
        Isolate::Scope isolate_scope(isolate_);
        managed_template_->Dispose();
        delete managed_template_;
        context_->Dispose();            
        delete context_;
    }

    isolate_->Dispose();
}

void JsEngine::DisposeObject(Persistent<Object>* obj)
{
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
    
    obj->Dispose();
    
    (*context_)->Exit();
}

jsvalue JsEngine::Execute(const uint16_t* str)
{
    jsvalue v;

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
        
    HandleScope scope;
    TryCatch trycatch;
        
    Handle<String> source = String::New(str);    
    Handle<Script> script = Script::Compile(source);          
    if (!script.IsEmpty()) {
        Local<Value> result = script->Run();
        if (result.IsEmpty())
            v = ErrorFromV8(trycatch);
        else
            v = AnyFromV8(result);        
    }
    else {
        v = ErrorFromV8(trycatch);
    }
            
    (*context_)->Exit();

    return v;     
}

jsvalue JsEngine::SetVariable(const uint16_t* name, jsvalue value)
{
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
        
    HandleScope scope;
        
    Handle<Value> v = AnyToV8(value);

    if ((*context_)->Global()->Set(String::New(name), v) == false) {
        // TODO: Return an error if set failed.
    }        
      
    (*context_)->Exit();
    
    return AnyFromV8(Null());
}

jsvalue JsEngine::GetVariable(const uint16_t* name)
{
    jsvalue v;
    
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
        
    HandleScope scope;
    TryCatch trycatch;
                
    Local<Value> value = (*context_)->Global()->Get(String::New(name));
    if (!value.IsEmpty()) {
        v = AnyFromV8(value);        
    }
    else {
        v = ErrorFromV8(trycatch);
    }
    
    (*context_)->Exit();
    
    return v;
}

jsvalue JsEngine::GetPropertyValue(Persistent<Object>* obj, const uint16_t* name)
{
    jsvalue v;
    
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
        
    HandleScope scope;
    TryCatch trycatch;
                
    Local<Value> value = (*obj)->Get(String::New(name));
    if (!value.IsEmpty()) {
        v = AnyFromV8(value);        
    }
    else {
        v = ErrorFromV8(trycatch);
    }
    
    (*context_)->Exit();
    
    return v;
}

jsvalue JsEngine::SetPropertyValue(Persistent<Object>* obj, const uint16_t* name, jsvalue value)
{
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
        
    HandleScope scope;
        
    Handle<Value> v = AnyToV8(value);

    if ((*obj)->Set(String::New(name), v) == false) {
        // TODO: Return an error if set failed.
    }          
    
    (*context_)->Exit();
    
    return AnyFromV8(Null());
}

jsvalue JsEngine::InvokeProperty(Persistent<Object>* obj, const uint16_t* name, jsvalue args)
{
    jsvalue v;

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    (*context_)->Enter();
        
    HandleScope scope;    
    TryCatch trycatch;
        
    Local<Value> prop = (*obj)->Get(String::New(name));
    if (prop.IsEmpty() || !prop->IsFunction()) {
        v = StringFromV8(String::New("property not found or isn't a function"));
        v.type = JSVALUE_TYPE_ERROR;   
    }
    else {
        Local<Value> argv[args.length];
        ArrayToV8Args(args, argv);
        // TODO: Check ArrayToV8Args return value (but right now can't fail, right?)                   
        Local<Function> func = Local<Function>::Cast(prop);
        Local<Value> value = func->Call(*obj, args.length, argv);
        if (!value.IsEmpty()) {
            v = AnyFromV8(value);        
        }
        else {
            v = ErrorFromV8(trycatch);
        }         
    }
    
    (*context_)->Exit();
    
    return v;
}

jsvalue JsEngine::ErrorFromV8(TryCatch& trycatch)
{
    jsvalue v;

    HandleScope scope;
    
    Local<Value> exception = trycatch.Exception();

    v.type = JSVALUE_TYPE_UNKNOWN_ERROR;        
    v.value.str = 0;
    v.length = 0;
    
    // If this is a managed exception we need to place its ID inside the jsvalue
    // and set the type JSVALUE_TYPE_MANAGED_ERROR to make sure the CLR side will
    // throw on it. Else we just wrap and return the exception Object. Note that
    // this is far from perfect because we ignore both the Message object and the
    // stack stack trace. If the exception is not an object (but just a string,
    // for example) we convert it with toString() and return that as an Exception.
    // TODO: return a composite/special object with stack trace information.
    
    if (exception->IsObject()) {
        Local<Object> obj = Local<Object>::Cast(exception);
        if (obj->InternalFieldCount() == 1) {
            ManagedRef* ref = (ManagedRef*)obj->GetPointerFromInternalField(0); 
            v.type = JSVALUE_TYPE_MANAGED_ERROR;
            v.length = ref->Id();
        }
        else  {
            v = WrappedFromV8(obj);
            v.type = JSVALUE_TYPE_WRAPPED_ERROR;        
        }            
    }
    else if (!exception.IsEmpty()) {
        v = StringFromV8(exception);
        v.type = JSVALUE_TYPE_ERROR;   
    }
    
    return v;
}
    
jsvalue JsEngine::StringFromV8(Handle<Value> value)
{
    jsvalue v;
    
    Local<String> s = value->ToString();
    v.length = s->Length();
    v.value.str = new uint16_t[v.length+1];
    if (v.value.str != NULL) {
        s->Write(v.value.str);
        v.type = JSVALUE_TYPE_STRING;
    }

    return v;
}   

jsvalue JsEngine::WrappedFromV8(Handle<Object> obj)
{
    jsvalue v;
    
    v.type = JSVALUE_TYPE_WRAPPED;
    v.length = 0;
    
    // A Persistent<Object> is exactly the size of an IntPtr, right?
    // If not we're in deep deep trouble (on IA32 and AMD64 should be).
    // We should even cast it to void* because C++ doesn't allow to put
    // it in a union: going scary and scarier here.    
    v.value.ptr = new Persistent<Object>(Persistent<Object>::New(obj));

    return v;
} 

jsvalue JsEngine::ManagedFromV8(Handle<Object> obj)
{
    jsvalue v;
    
    ManagedRef* ref = (ManagedRef*)obj->GetPointerFromInternalField(0); 
    v.type = JSVALUE_TYPE_MANAGED;
    v.length = ref->Id();
    v.value.str = 0;

    return v;
}
    
jsvalue JsEngine::AnyFromV8(Handle<Value> value)
{
    jsvalue v;
    
    // Initialize to a generic error.
    v.type = JSVALUE_TYPE_UNKNOWN_ERROR;
    v.length = 0;
    v.value.str = 0;
    
    if (value->IsNull() || value->IsUndefined()) {
        v.type = JSVALUE_TYPE_NULL;
    }                
    else if (value->IsBoolean()) {
        v.type = JSVALUE_TYPE_BOOLEAN;
        v.value.i32 = value->BooleanValue() ? 1 : 0;
    }
    else if (value->IsInt32()) {
        v.type = JSVALUE_TYPE_INTEGER;
        v.value.i32 = value->Int32Value();            
    }
    else if (value->IsUint32()) {
        v.type = JSVALUE_TYPE_INDEX;
        v.value.i64 = value->Uint32Value();            
    }
    else if (value->IsNumber()) {
        v.type = JSVALUE_TYPE_NUMBER;
        v.value.num = value->NumberValue();
    }
    else if (value->IsString()) {
        v = StringFromV8(value);
    }
    else if (value->IsDate()) {
        v.type = JSVALUE_TYPE_DATE;
        v.value.num = value->NumberValue();
    }
    else if (value->IsArray()) {
        Handle<Array> object = Handle<Array>::Cast(value->ToObject());
        v.length = object->Length();
        jsvalue* array = new jsvalue[v.length];
        if (array != NULL) {
            for(int i = 0; i < v.length; i++) {
                array[i] = AnyFromV8(object->Get(i));
            }
            v.type = JSVALUE_TYPE_ARRAY;
            v.value.arr = array;
        }
    }
    else if (value->IsFunction()) {
        // TODO: how do we represent this on the CLR side? Delegate?
    }
    else if (value->IsObject()) {
        Handle<Object> obj = Handle<Object>::Cast(value);
        if (obj->InternalFieldCount() ==     1)
            v = ManagedFromV8(obj);
        else
            v = WrappedFromV8(obj);
    }

    return v;
}

Handle<Value> JsEngine::AnyToV8(jsvalue v)
{
    if (v.type == JSVALUE_TYPE_NULL) {
        return Null();
    }
    if (v.type == JSVALUE_TYPE_BOOLEAN) {
        return Boolean::New(v.value.i32);
    }
    if (v.type == JSVALUE_TYPE_INTEGER) {
        return Int32::New(v.value.i32);
    }
    if (v.type == JSVALUE_TYPE_NUMBER) {
        return Number::New(v.value.num);
    }
    if (v.type == JSVALUE_TYPE_STRING) {
        return String::New(v.value.str);
    }
    if (v.type == JSVALUE_TYPE_DATE) {
        return Date::New(v.value.num);
    }

    // Arrays are converted to JS native arrays.
    
    if (v.type == JSVALUE_TYPE_ARRAY) {
        Local<Array> a = Array::New(v.length);
        for(int i = 0; i < v.length; i++) {
            a->Set(i, AnyToV8(v.value.arr[i]));
        }
        return a;        
    }
        
    // This is an ID to a managed object that lives inside the JsEngine keep-alive
    // cache. We just wrap it and the pointer to the engine inside an External. A
    // managed error is still a CLR object so it is wrapped exactly as a normal
    // managed object.
    
    if (v.type == JSVALUE_TYPE_MANAGED || v.type == JSVALUE_TYPE_MANAGED_ERROR) {
        ManagedRef* ref = new ManagedRef(this, v.length);
        Persistent<Object> obj = Persistent<Object>::New((*(managed_template_))->NewInstance());
        obj->SetInternalField(0, External::New(ref));
        obj.MakeWeak(NULL, managed_destroy);
        return obj;
    }

    return Null();
}

int32_t JsEngine::ArrayToV8Args(jsvalue value, Handle<Value> preallocatedArgs[])
{
    if (value.type != JSVALUE_TYPE_ARRAY)
        return -1;
        
    for (int i=0 ; i < value.length ; i++) {
        preallocatedArgs[i] = AnyToV8(value.value.arr[i]);
    }
    
    return value.length;
}

jsvalue JsEngine::ArrayFromArguments(const Arguments& args)
{
    jsvalue v = jsvalue_alloc_array(args.Length());
    
    for (int i=0 ; i < v.length ; i++) {
        v.value.arr[i] = AnyFromV8(args[i]);
    }
    
    return v;
}