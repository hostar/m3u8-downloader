using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class UserAgentParameterEvaluator : ParameterEvaluator
    {
        public UserAgentParameterEvaluator()
        {
            Keys = new HashSet<string> { "-A", "--user-agent" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var value = commandLine.ReadValue();

            convertResult.Data.SetHeader("User-Agent", value.ToString());
        }
    }
}
