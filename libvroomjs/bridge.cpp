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
    JsEngine* jsengine_new(keepalive_remove_f keepalive_remove, 
                           keepalive_get_property_value_f keepalive_get_property_value,
                           keepalive_set_property_value_f keepalive_set_property_value)
    {
        JsEngine* engine = JsEngine::New();
        if (engine != NULL) {
            engine->SetRemoveDelegate(keepalive_remove);
            engine->SetGetPropertyValueDelegate(keepalive_get_property_value);
            engine->SetSetPropertyValueDelegate(keepalive_set_property_value);
        }
        return engine;
    }

    void jsengine_dispose(JsEngine* engine)
    {
        engine->Dispose();        
        delete engine;
    }
    
    jsvalue jsengine_execute(JsEngine* engine, const uint16_t* str)
    {
        return engine->Execute(str);
    }
        
    jsvalue jsengine_set_variable(JsEngine* engine, const uint16_t* name, jsvalue value)
    {
        return engine->SetVariable(name, value);
    }

    jsvalue jsengine_get_variable(JsEngine* engine, const uint16_t* name)
    {
        return engine->GetVariable(name);
    }

    jsvalue jsvalue_alloc_string(const uint16_t* str)
    {
        jsvalue v;
    
        int length = 0;
        while (str[length] != '\0')
            length++;
          
        v.length = length;
        v.value.str = new uint16_t[length+1];
        if (v.value.str != NULL) {
            for (int i=0 ; i < length ; i++)
                 v.value.str[i] = str[i];
            v.value.str[length] = '\0';
            v.type = JSVALUE_TYPE_STRING;
        }

        return v;
    }    
                
    void jsvalue_dispose(jsvalue value)
    {
        if (value.type == JSVALUE_TYPE_STRING || value.type == JSVALUE_TYPE_UNKNOWN_ERROR) {
            if (value.value.str != NULL)
                delete value.value.str;
        }
        else if (value.type == JSVALUE_TYPE_ARRAY) {
            for (int i=0 ; i < value.length ; i++)
                jsvalue_dispose(value.value.arr[i]);
            if (value.value.arr != NULL)
                delete value.value.arr;
        }            
    }        
}
