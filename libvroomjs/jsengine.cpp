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

extern "C" 
{
    jsengine* jsengine_new()
    {
        jsengine* engine = new jsengine();
        if (engine != NULL) {
            engine->isolate = Isolate::New();
            Locker v8ThreadLock(engine->isolate);
            Isolate::Scope isolate_scope(engine->isolate);
            engine->context = new Persistent<Context>(Context::New());
        }
        
        return engine;
    }
    
    void jsengine_dispose(jsengine* engine)
    {
        {
            Locker v8ThreadLock(engine->isolate);
            Isolate::Scope isolate_scope(engine->isolate);
            engine->context->Dispose();
            delete engine->context;
        }
        if (engine->isolate != NULL)
            engine->isolate->Dispose();
        
        delete engine;
    }
    
    jsvalue jsengine_execute(jsengine* engine, const uint16_t* str)
    {
        jsvalue v;

        Locker* locker = new Locker(engine->isolate);
        engine->isolate->Enter();
        (*(engine->context))->Enter();
            
        HandleScope scope;
            
        Handle<String> source = String::New(str);

        TryCatch trycatch;
        
        Handle<Script> script = Script::Compile(source);          
        if (!script.IsEmpty()) {
            Local<Value> result = script->Run();
            if (result.IsEmpty())
                v = jsvalue_error_fromV8(trycatch);
            else
                v = jsvalue_any_fromV8(result);        
        }
        else {
            v = jsvalue_error_fromV8(trycatch);
        }
                
        (*(engine->context))->Exit();
        engine->isolate->Exit();
    
        delete locker;   
        
        return v;     
    }
        
    void jsengine_set(jsengine* engine, const uint16_t* name, jsvalue value)
    {
        Locker* locker = new Locker(engine->isolate);
        engine->isolate->Enter();
        (*(engine->context))->Enter();
            
        HandleScope scope;
            
        Handle<Value> v = jsvalue_toV8(value);

        (*(engine->context))->Global()->Set(String::New(name), v);  
        
        (*(engine->context))->Exit();
        engine->isolate->Exit();
    
        delete locker;      
    }
    
    void jsengine_free(jsvalue value)
    {
        if (value.type == JSVALUE_TYPE_STRING || value.type == JSVALUE_TYPE_ERROR) {
            if (value.value.str != NULL)
                delete value.value.str;
        }
        else if (value.type == JSVALUE_TYPE_ARRAY) {
            for (int i=0 ; i < value.length ; i++)
                jsengine_free(value.value.arr[i]);
            if (value.value.arr != NULL)
                delete value.value.arr;
        }            
    }    
    
}
