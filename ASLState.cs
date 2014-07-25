using LiveSplit.OcarinaOfTime;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace LiveSplit.ASL
{
    public class ASLState : ICloneable
    {
        public ExpandoObject Data { get; set; }
        public List<ASLValueDefinition> ValueDefinitions { get; set; }

        public ASLState()
        {
            Data = new ExpandoObject();
            ValueDefinitions = new List<ASLValueDefinition>();
        }

        public ASLState RefreshValues()
        {
            var clone = (ASLState)Clone();

            var dict = ((IDictionary<string, object>)Data);
            foreach (var valueDefinition in ValueDefinitions)
            {
                var value = ~valueDefinition.Pointer;
                if (dict.ContainsKey(valueDefinition.Identifier))
                {
                    dict[valueDefinition.Identifier] = value;
                }
                else
                {
                    dict.Add(valueDefinition.Identifier, value);
                }
            }
            return clone;
        }

        public object Clone()
        {
            var clone = new ExpandoObject();
            foreach (var pair in (IDictionary<string, object>)Data)
            {
                ((IDictionary<string, object>)clone).Add(pair);
            }
            return new ASLState() { Data = clone, ValueDefinitions = new List<ASLValueDefinition>(ValueDefinitions) };
        }
    }
}
