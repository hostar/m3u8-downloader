using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurlToCSharp.Extensions;
using CurlToCSharp.Constants;
using CurlToCSharp.Models;
using CurlToCSharp.Models.Parsing;

namespace m3u8_downloader_photino.Libs.curl
{
    public class CommandLineParser
    {
        private readonly IEnumerable<ParameterEvaluator> _evaluators;

        public CommandLineParser(ParsingOptions parsingOptions)
            : this(EvaluatorProvider.All(parsingOptions))
        {
        }

        private CommandLineParser(IEnumerable<ParameterEvaluator> evaluators)
        {
            _evaluators = evaluators;
        }

        public ConvertResult<CurlOptions> Parse(Span<char> commandLine)
        {
            if (commandLine.IsEmpty)
            {
                throw new ArgumentException("The command line is empty.", nameof(commandLine));
            }

            var parseResult = new ConvertResult<CurlOptions>(new CurlOptions());
            var parseState = new ParseState();
            while (!commandLine.IsEmpty)
            {
                commandLine = commandLine.TrimCommandLine();
                if (commandLine.IsEmpty)
                {
                    break;
                }

                if (commandLine.IsParameter())
                {
                    var parameter = commandLine.ReadParameter();
                    //EvaluateParameter(parameter, ref commandLine, parseResult);
                    EvaluateParameter2(parameter.ToString(), ref commandLine, parseResult);
                }
                else
                {
                    var value = commandLine.ReadValue();
                    EvaluateValue(parseResult, parseState, value);
                }
            }

            PostParsing(parseResult, parseState);

            return parseResult;
        }

        private static void EvaluateValue(ConvertResult<CurlOptions> convertResult, ParseState parseState, Span<char> value)
        {
            var valueString = value.ToString();
            if (string.Equals(valueString, "curl", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (convertResult.Data.Url == null && Uri.TryCreate(valueString, UriKind.Absolute, out var url)
                                                  && !string.IsNullOrEmpty(url.Host))
            {
                convertResult.Data.Url = url;
            }
            else
            {
                parseState.LastUnknownValue = valueString;
            }
        }

        private void EvaluateParameter(Span<char> parameter, ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var par = parameter.ToString();

            foreach (var evaluator in _evaluators)
            {
                if (evaluator.CanEvaluate(par))
                {
                    evaluator.Evaluate(ref commandLine, convertResult);

                    return;
                }
            }

            convertResult.Warnings.Add(Messages.GetParameterIsNotSupported(par));
        }

        private void EvaluateParameter2(string parameter, ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            foreach (var evaluator in _evaluators)
            {
                if (evaluator.CanEvaluate(parameter))
                {
                    evaluator.Evaluate(ref commandLine, convertResult);

                    return;
                }
            }

            convertResult.Warnings.Add(Messages.GetParameterIsNotSupported(parameter));
        }

        private void PostParsing(ConvertResult<CurlOptions> result, ParseState state)
        {
            if (result.Data.Url == null
                && !string.IsNullOrWhiteSpace(state.LastUnknownValue)
                && Uri.TryCreate($"http://{state.LastUnknownValue}", UriKind.Absolute, out Uri url))
            {
                result.Data.Url = url;
            }

            // This option overrides -F, --form and -I, --head and -T, --upload-file.
            if (result.Data.HasDataPayload)
            {
                result.Data.UploadFiles.Clear();
                result.Data.FormData.Clear();
            }

            if (result.Data.HasFormPayload)
            {
                result.Data.UploadFiles.Clear();
            }

            // If used in combination with -I, --head, the POST data will instead be appended to the URL with a HEAD request.
            if (result.Data.ForceGet && result.Data.HttpMethod != "HEAD")
            {
                result.Data.HttpMethod = "GET";
            }

            if (result.Data.HttpMethod == null)
            {
                if (result.Data.HasDataPayload)
                {
                    result.Data.HttpMethod = "POST";
                }
                else if (result.Data.HasFormPayload)
                {
                    result.Data.HttpMethod = "POST";
                }
                else if (result.Data.HasFilePayload)
                {
                    result.Data.HttpMethod = "PUT";
                }
                else
                {
                    result.Data.HttpMethod = "GET";
                }
            }

            if (!result.Data.HasHeader("Content-Type") && result.Data.HasDataPayload)
            {
                result.Data.SetHeader("Content-Type", HeaderValues.ContentTypeWwwForm);
            }

            if (result.Data.Url == null)
            {
                result.Errors.Add(Messages.UnableParseUrl);
            }
        }
    }
}
