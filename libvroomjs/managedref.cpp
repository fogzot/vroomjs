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

Handle<Value> ManagedRef::GetPropertyValue(Local<String> name)
{
    Handle<Value> res;
    
    String::Value s(name);
    
    jsvalue r = engine_->CallGetPropertyValue(id_, *s);
    if (r.type == JSVALUE_TYPE_MANAGED_ERROR)
        res = ThrowException(engine_->AnyToV8(r));
    else
        res = engine_->AnyToV8(r);
    
    // We don't need the jsvalue anymore and the CLR side never reuse them.
    jsvalue_dispose(r);
    
    return res;
}

Handle<Value> ManagedRef::SetPropertyValue(Local<String> name, Local<Value> value)
{
    Handle<Value> res;
    
    String::Value s(name);
    
    jsvalue v = engine_->AnyFromV8(value);
    jsvalue r = engine_->CallSetPropertyValue(id_, *s, v);
    if (r.type == JSVALUE_TYPE_MANAGED_ERROR)
        res = ThrowException(engine_->AnyToV8(r));
    else
        res = engine_->AnyToV8(r);
    
    // We don't need the jsvalues anymore and the CLR side never reuse them.
    jsvalue_dispose(v);
    jsvalue_dispose(r);
    
    return res;
}