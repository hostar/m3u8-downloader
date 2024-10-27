﻿using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class UrlParameterEvaluator : ParameterEvaluator
    {
        public UrlParameterEvaluator()
        {
            Keys = new HashSet<string> { "--url" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var value = commandLine.ReadValue();
            var stringValue = value.ToString();
            if (Uri.TryCreate(stringValue, UriKind.Absolute, out var url) || Uri.TryCreate(
                    $"http://{stringValue}",
                    UriKind.Absolute,
                    out url))
            {
                convertResult.Data.Url = url;
            }
            else
            {
                convertResult.Warnings.Add($"Unable to parse URL \"{stringValue}\"");
            }
        }
    }
}
