// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright (c) 2013 
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

#ifndef LIBVROOMJS_H
#define LIBVROOMJS_H 1

#include <v8.h>
#include <stdlib.h>
#include <stdint.h>

extern "C" 
{
    struct jsengine 
    {
        v8::Isolate                    *isolate;
        v8::Persistent<v8::Context>    *context;
    };
    
    struct jsvalue
    {
        // 8 bytes is the maximum CLR alignment; by putting the union first we make
        // (almost) sure the offset of 'type' will always be 8.
        union 
        {
            int32_t     i32;
            int64_t     i64;
            double      num;
            uint16_t   *str;
            jsvalue    *arr;
        } value;
        
        int32_t         type;
        int32_t         length;
    };  
    
    extern jsvalue jsvalue_any_fromV8(v8::Handle<v8::Value> value);
    extern jsvalue jsvalue_string_fromV8(v8::Handle<v8::Value> value);
    extern jsvalue jsvalue_error_fromV8(v8::TryCatch& trycatch);
    extern v8::Handle<v8::Value> jsvalue_toV8(jsvalue value);
}

#define JSVALUE_TYPE_ERROR      0xFFFFFFFF
#define JSVALUE_TYPE_NULL       0
#define JSVALUE_TYPE_BOOLEAN    1
#define JSVALUE_TYPE_INTEGER    2
#define JSVALUE_TYPE_NUMBER     3
#define JSVALUE_TYPE_STRING     4
#define JSVALUE_TYPE_DATE       5
#define JSVALUE_TYPE_ARRAY      13
#define JSVALUE_TYPE_OBJECT     14
#define JSVALUE_TYPE_WRAPPED    15

#endif