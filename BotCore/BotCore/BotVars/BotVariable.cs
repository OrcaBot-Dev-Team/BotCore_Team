using JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.BotVars
{
    public struct BotVar
    {
        public string Identifier;
        public BotVarType Type;

        public ulong UInt64;
        public long Int64;
        public double Float64;
        public string String;
        public bool Bool;
        public JSONContainer Generic;

        public BotVar(string id, ulong var)
        {
            Identifier = id;
            Type = BotVarType.UInt64;
            UInt64 = var;
            Int64 = default;
            Float64 = default;
            String = default;
            Bool = default;
            Generic = default;
        }

        public BotVar(string id, long var)
        {
            Identifier = id;
            Type = BotVarType.Int64;
            Int64 = var;
            UInt64 = default;
            Float64 = default;
            String = default;
            Bool = default;
            Generic = default;
        }
        public BotVar(string id, double var)
        {
            Identifier = id;
            Type = BotVarType.Float64;
            Float64 = var;
            UInt64 = default;
            Int64 = default;
            String = default;
            Bool = default;
            Generic = default;
        }

        public BotVar(string id, string var)
        {
            Identifier = id;
            Type = BotVarType.String;
            String = var;
            UInt64 = default;
            Int64 = default;
            Float64 = default;
            Bool = default;
            Generic = default;
        }
        public BotVar(string id, bool var)
        {
            Identifier = id;
            Type = BotVarType.Bool;
            Bool = var;
            UInt64 = default;
            Int64 = default;
            Float64 = default;
            String = default;
            Generic = default;
        }

        public BotVar(string id, JSONContainer var)
        {
            Identifier = id;
            Type = BotVarType.Generic;
            Generic = var;
            UInt64 = default;
            Int64 = default;
            Float64 = default;
            String = default;
            Bool = default;
        }

        public BotVar(string id, IGenericBotVar var)
        {
            Identifier = id;
            Type = BotVarType.Generic;
            Generic = var.ToJSON();
            UInt64 = default;
            Int64 = default;
            Float64 = default;
            String = default;
            Bool = default;
        }

        private const string JSON_ID = "Id";
        private const string JSON_TYPE = "Type";
        private const string JSON_VALUE = "Val";

        internal JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            result.TryAddField(JSON_ID, Identifier);
            result.TryAddField(JSON_TYPE, (int)Type);
            switch (Type)
            {
                case BotVarType.UInt64:
                    result.TryAddField(JSON_VALUE, UInt64);
                    break;
                case BotVarType.Int64:
                    result.TryAddField(JSON_VALUE, Int64);
                    break;
                case BotVarType.Float64:
                    result.TryAddField(JSON_VALUE, Float64);
                    break;
                case BotVarType.String:
                    result.TryAddField(JSON_VALUE, String);
                    break;
                case BotVarType.Bool:
                    result.TryAddField(JSON_VALUE, Bool);
                    break;
                case BotVarType.Generic:
                    result.TryAddField(JSON_VALUE, Generic);
                    break;
            }
            return result;
        }

        internal bool FromJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_ID, out Identifier) && json.TryGetField(JSON_TYPE, out int type))
            {
                Type = (BotVarType)type;
                switch (Type)
                {
                    case BotVarType.Undefined:
                        return true;
                    case BotVarType.UInt64:
                        return json.TryGetField(JSON_VALUE, out UInt64);
                    case BotVarType.Int64:
                        return json.TryGetField(JSON_VALUE, out Int64);
                    case BotVarType.Float64:
                        return json.TryGetField(JSON_VALUE, out Float64);
                    case BotVarType.String:
                        return json.TryGetField(JSON_VALUE, out String);
                    case BotVarType.Bool:
                        return json.TryGetField(JSON_VALUE, out Bool);
                    case BotVarType.Generic:
                        return json.TryGetField(JSON_VALUE, out Generic);
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case BotVarType.Undefined:
                    return $"Type: `{Type}`";
                case BotVarType.UInt64:
                    return $"Type: `{Type}`\nValue: `{UInt64}`";
                case BotVarType.Int64:
                    return $"Type: `{Type}`\nValue: `{Int64}`";
                case BotVarType.Float64:
                    return $"Type: `{Type}`\nValue: `{Float64}`";
                case BotVarType.String:
                    return $"Type: `{Type}`\nValue: ```{String}```";
                case BotVarType.Bool:
                    return $"Type: `{Type}`\nValue: `{Bool}`";
                case BotVarType.Generic:
                    return $"Type: `{Type}`\nValue: ```{Generic.Build(true)}```";
                default:
                    return base.ToString();
            }
        }

        public bool IsDefined { get { return TypeNullTested != BotVarType.Undefined; } }
        public bool IsUnsignedInt64 { get { return Type == BotVarType.UInt64; } }
        public bool IsSignedInt64 { get { return Type == BotVarType.Int64; } }
        public bool IsFloat64 { get { return Type == BotVarType.Float64; } }
        public bool IsString { get { return Type == BotVarType.String && String != null; } }
        public bool IsBool { get { return Type == BotVarType.Bool; } }
        public bool IsGeneric { get { return Type == BotVarType.Generic && Generic != null; } }
        public BotVarType TypeNullTested
        {
            get
            {
                switch (Type)
                {
                    case BotVarType.String:
                        if (String == null)
                        {
                            return BotVarType.Undefined;
                        }
                        break;
                    case BotVarType.Generic:
                        if (Generic == null)
                        {
                            return BotVarType.Undefined;
                        }
                        break;
                }
                return Type;
            }
        }

        public bool TryConvert<T>(out T result) where T : class, IGenericBotVar, new()
        {
            if (IsGeneric)
            {
                result = new T();
                if (result.ApplyJSON(Generic))
                {
                    return true;
                }
            }
            result = null;
            return false;
        }
    }

    public enum BotVarType
    {
        Undefined,
        UInt64,
        Int64,
        Float64,
        String,
        Bool,
        Generic
    }
}
