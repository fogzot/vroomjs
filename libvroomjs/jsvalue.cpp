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
#include <string>

using namespace v8;

extern "C" 
{    
    jsvalue jsvalue_string_fromV8(Handle<Value> value)
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

    jsvalue jsvalue_error_fromV8(TryCatch& trycatch)
    {
        jsvalue v;

        Handle<Value> exception = trycatch.Exception();
        v = jsvalue_string_fromV8(exception);
        v.type = JSVALUE_TYPE_ERROR;
        
        return v;
    }
    
    jsvalue jsvalue_any_fromV8(Handle<Value> value)
    {
        jsvalue v;
        
        // Initialize to a generic error.
        v.type = JSVALUE_TYPE_ERROR;
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
        else if (value->IsNumber()) {
            v.type = JSVALUE_TYPE_NUMBER;
            v.value.num = value->NumberValue();
        }
        else if (value->IsString()) {
            v = jsvalue_string_fromV8(value);
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
                    array[i] = jsvalue_any_fromV8(object->Get(i));
                }
                v.type = JSVALUE_TYPE_ARRAY;
                v.value.arr = array;
            }
        }

        return v;
    }
    
    Handle<Value> jsvalue_toV8(jsvalue v)
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
    
        return Null();
    }
    
}
