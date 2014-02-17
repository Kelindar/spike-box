﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.FSharp.Collections;

namespace Spike.Scripting.Runtime
{
    public class Environment
    {
        ulong currentSchemaId = 0;
        ulong currentFunctionId = 0;

        readonly Dictionary<ulong, FunctionMetaData> functionMetaData =
            new Dictionary<ulong, FunctionMetaData>();

        internal readonly Caches.WeakCache<Tuple<RegexOptions, string>, Regex> RegExpCache
            = new Caches.WeakCache<Tuple<RegexOptions, string>, Regex>();

        public readonly Caches.LimitCache<string, EvalCode> EvalCache
            = new Caches.LimitCache<string, EvalCode>(100);

        public BoxedValue Return;
        public ScriptObject Globals;
        public int Line;
        public Action<int, int, Dictionary<string, object>> BreakPoint;

        public Maps Maps;
        public readonly Random Random = new Random();
        public readonly Prototypes Prototypes = Prototypes.Empty;
        public readonly Constructors Constructors = Constructors.Empty;

        public static BoxedValue BoxedZero { get { return BoxedValue.Box(0.0); } }
        public static BoxedValue BoxedNull { get { return BoxedValue.Box(null, TypeTags.Clr); } }

        public Environment()
        {
            functionMetaData.Add(0UL, null);
        }

        public ulong NextFunctionId()
        {
            return ++currentFunctionId;
        }

        public ulong NextPropertyMapId()
        {
            return ++currentSchemaId;
        }

        public FunctionMetaData GetFunctionMetaData(ulong id)
        {
            return functionMetaData[id];
        }

        public bool HasFunctionMetaData(ulong id)
        {
            return functionMetaData.ContainsKey(id);
        }

        public void AddFunctionMetaData(FunctionMetaData metaData)
        {
            functionMetaData[metaData.Id] = metaData;
        }

        public FunctionMetaData CreateHostMetaData(FunctionType functionType, FunctionCompiler compiler)
        {
            var id = NextFunctionId();
            var metaData = new FunctionMetaData(id, functionType, compiler);
            AddFunctionMetaData(metaData);
            return metaData;
        }

        public FunctionMetaData CreateHostConstructorMetaData(FunctionCompiler compiler)
        {
            return CreateHostMetaData(FunctionType.NativeConstructor, compiler);
        }

        public FunctionMetaData CreateHostFunctionMetaData(FunctionCompiler compiler)
        {
            return CreateHostMetaData(FunctionType.NativeFunction, compiler);
        }

        public ScriptObject NewObject()
        {
            return new ScriptObject(this, Maps.Base, Prototypes.Object);
        }

        public ScriptObject NewMath()
        {
            return new MathObject(this);
        }

        public ScriptObject NewArray()
        {
            return NewArray(0);
        }

        public ScriptObject NewArray(uint size)
        {
            return new ArrayObject(this, size) { Length = size };
        }

        public ScriptObject NewDate(DateTime date)
        {
            return new DateObject(this, date);
        }

        public ScriptObject NewBoolean()
        {
            return this.NewBoolean(false);
        }

        public ScriptObject NewBoolean(bool value)
        {
            BooleanObject boolean = new BooleanObject(this);
            boolean.Value.Value.Bool = value;
            boolean.Value.Value.Tag = 0xffffff01;
            boolean.Value.HasValue = true;
            return boolean;
        }

        public ErrorObject NewError()
        {
            return new ErrorObject(this);
        }

        public FunctionObject NewFunction(ulong id, int args, BoxedValue[] closureScope, FSharpList<Tuple<int, ScriptObject>> dynamicScope)
        {
            FunctionObject func = new FunctionObject(this, id, closureScope, dynamicScope);
            ScriptObject proto = this.NewPrototype();
            proto.Put("constructor", func, 2);
            func.Put("prototype", proto, 4);
            func.Put("length", (double)args, 7);
            return func;
        }

        public ScriptObject NewNumber()
        {
            return this.NewNumber(0.0);
        }

        public ScriptObject NewNumber(double value)
        {
            NumberObject number = new NumberObject(this);
            number.Value.Value.Number = value;
            number.Value.HasValue = true;
            return number;
        }

        public ScriptObject NewPrototype()
        {
            return new ScriptObject(this, this.Maps.Prototype, this.Prototypes.Object);
        }

        public ScriptObject NewRegExp()
        {
            return this.NewRegExp("");
        }
        public ScriptObject NewRegExp(string pattern)
        {
            return this.NewRegExp(pattern, "");
        }

        public ScriptObject NewRegExp(string pattern, string options)
        {
            pattern = pattern ?? "";
            options = options ?? "";

            var multiline = false;
            var ignoreCase = false;
            var global = false;

            foreach (Char o in options)
            {
                if (o == 'm' && !multiline) multiline = true;
                else if (o == 'i' && !ignoreCase) ignoreCase = true;
                else if (o == 'g' && !global) global = true;
                else return RaiseSyntaxError<ScriptObject>("Invalid RegExp options '" + options + "'");
            }

            var opts = RegexOptions.None;

            if (multiline)
                opts |= RegexOptions.Multiline;

            if (ignoreCase)
                opts |= RegexOptions.IgnoreCase;

            return NewRegExp(pattern, opts, global);
        }

        public ScriptObject NewRegExp(string pattern, RegexOptions options, bool isGlobal)
        {
            RegExpObject regexp = new RegExpObject(this, pattern, options, isGlobal);
            regexp.Put("source", pattern, 7);
            regexp.Put("global", isGlobal, 7);
            regexp.Put("ignoreCase", regexp.IgnoreCase, 7);
            regexp.Put("multiline", regexp.MultiLine, 7);
            regexp.Put("lastIndex", (double)0.0, (ushort)6);
            return regexp;
        }

        public ScriptObject NewString()
        {
            return this.NewString(string.Empty);
        }

        public ScriptObject NewString(string value)
        {
            StringObject @string = new StringObject(this);
            @string.Properties[0].Value.Number = value.Length;
            @string.Properties[0].Attributes = 3;
            @string.Properties[0].HasValue = true;
            @string.Value.Value.Clr = value;
            @string.Value.Value.Tag = 0xffffff04;
            @string.Value.HasValue = true;
            return @string;
        }

        public T RaiseError<T>(ScriptObject prototype, string message)
        {
            ErrorObject error = new ErrorObject(this)
            {
                Prototype = prototype
            };

            error.Put("message", message);
            throw new UserError(BoxedValue.Box((ScriptObject)error), 0, 0);
        }

        public T RaiseEvalError<T>()
        {
            return this.RaiseEvalError<T>("");
        }

        public T RaiseEvalError<T>(string message)
        {
            return this.RaiseError<T>(this.Prototypes.EvalError, message);
        }

        public T RaiseRangeError<T>()
        {
            return this.RaiseRangeError<T>("");
        }

        public T RaiseRangeError<T>(string message)
        {
            return this.RaiseError<T>(this.Prototypes.RangeError, message);
        }

        public T RaiseReferenceError<T>()
        {
            return this.RaiseReferenceError<T>("");
        }

        public T RaiseReferenceError<T>(string message)
        {
            return this.RaiseError<T>(this.Prototypes.ReferenceError, message);
        }

        public T RaiseSyntaxError<T>()
        {
            return this.RaiseSyntaxError<T>("");
        }

        public T RaiseSyntaxError<T>(string message)
        {
            return this.RaiseError<T>(this.Prototypes.SyntaxError, message);
        }

        public T RaiseTypeError<T>()
        {
            return this.RaiseTypeError<T>("");
        }

        public T RaiseTypeError<T>(string message)
        {
            return this.RaiseError<T>(this.Prototypes.TypeError, message);
        }

        public T RaiseURIError<T>()
        {
            return this.RaiseURIError<T>("");
        }

        public T RaiseURIError<T>(string message)
        {
            return this.RaiseError<T>(this.Prototypes.URIError, message);
        }
    }

    public class Prototypes
    {
        public ScriptObject Object;
        public ScriptObject Array;
        public FunctionObject Function;
        public ScriptObject String;
        public ScriptObject Number;
        public ScriptObject Boolean;
        public ScriptObject Date;
        public ScriptObject RegExp;
        public ScriptObject Error;
        public ScriptObject EvalError;
        public ScriptObject RangeError;
        public ScriptObject ReferenceError;
        public ScriptObject SyntaxError;
        public ScriptObject TypeError;
        public ScriptObject URIError;

        public static Prototypes Empty
        {
            get
            {
                return new Prototypes();
            }
        }
    }

    public class Constructors
    {
        public FunctionObject Object;
        public FunctionObject Array;
        public FunctionObject Function;
        public FunctionObject String;
        public FunctionObject Number;
        public FunctionObject Boolean;
        public FunctionObject Date;
        public FunctionObject RegExp;
        public FunctionObject Error;
        public FunctionObject EvalError;
        public FunctionObject RangeError;
        public FunctionObject ReferenceError;
        public FunctionObject SyntaxError;
        public FunctionObject TypeError;
        public FunctionObject URIError;

        public static Constructors Empty
        {
            get
            {
                return new Constructors();
            }
        }
    }

    public class Maps
    {
        public Schema Base;
        public Schema Array;
        public Schema Function;
        public Schema Prototype;
        public Schema String;
        public Schema Number;
        public Schema Boolean;
        public Schema RegExp;

        public static Maps Create(Schema baseSchema)
        {
            var maps = new Maps();

            maps.Base = baseSchema;
            maps.Array = baseSchema.SubClass("length");
            maps.Function = baseSchema.SubClass(new[] { "length", "prototype" });
            maps.Prototype = baseSchema.SubClass("constructor");
            maps.String = baseSchema.SubClass("length");
            maps.Number = baseSchema;
            maps.Boolean = baseSchema;
            maps.RegExp = baseSchema.SubClass(new[] { "source", "global", "ignoreCase", "multiline", "lastIndex" });

            return maps;
        }
    }
}
