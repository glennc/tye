// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Tye.ConfigModel;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Tye.Serialization
{
    public class YamlParser : IDisposable
    {
        private YamlStream _yamlStream;
        private FileInfo? _fileInfo;
        private TextReader _reader;

        public YamlParser(string yamlContent, FileInfo? fileInfo = null)
            : this(new StringReader(yamlContent), fileInfo)
        {
        }

        public YamlParser(FileInfo fileInfo)
            : this(fileInfo.OpenText(), fileInfo)
        {
        }

        internal YamlParser(TextReader reader, FileInfo? fileInfo = null)
        {
            _reader = reader;
            _yamlStream = new YamlStream();
            _fileInfo = fileInfo;
        }

        public ConfigApplication ParseConfigApplication()
        {
            try
            {
                _yamlStream.Load(_reader);
            }
            catch (YamlException ex)
            {
                throw new TyeYamlException(ex.Start, "Unable to parse tye.yaml. See inner exception.", ex);
            }

            var app = new ConfigApplication();

            // TODO assuming first document.
            var document = _yamlStream.Documents[0];
            var node = document.RootNode;
            ThrowIfNotYamlMapping(node);

            app.Source = _fileInfo!;

            ConfigApplicationParser.HandleConfigApplication((YamlMappingNode)node, app);

            app.Name ??= NameInferer.InferApplicationName(_fileInfo!);


            // TODO confirm if these are ever null.
            foreach (var service in app.Services)
            {
                service.Bindings ??= new List<ConfigServiceBinding>();
                service.Configuration ??= new List<ConfigConfigurationSource>();
                service.Volumes ??= new List<ConfigVolume>();
                service.Tags ??= new List<string>();

                foreach(var config in service.Configuration)
                {
                    config.Value = ReplaceValues(config.Value, service);
                }
            }



            foreach (var ingress in app.Ingress)
            {
                ingress.Bindings ??= new List<ConfigIngressBinding>();
                ingress.Rules ??= new List<ConfigIngressRule>();
                ingress.Tags ??= new List<string>();
            }

            return app;
        }



        public static string ReplaceValues(string value, ConfigService service)
        {
            var tokens = GetTokens(value);
            string newValue = value;
            foreach (var token in tokens)
            {
                var replacement = ResolveToken(token, service);
                if (replacement is null)
                {
                    throw new InvalidOperationException($"No available substitutions found for token '{token}'.");
                }

                newValue = value.Replace(token, replacement);
            }
            return newValue;
        }

        private static string ResolveToken(string token, ConfigService service)
        {
            var keys = token[2..^1].Split(':');
            if (keys.Length == 2 && keys[0] == "rand")
            {
                return Guid.NewGuid().ToString();
            }
            else if (keys.Length == 2 && keys[0] == "secret")
            {
                var secret = service.Configuration.FirstOrDefault(x => x.Name == keys[1]);
                if(secret is null)
                {
                    throw new Exception($"Unable to find secret {keys[1]}");
                }

                return secret.Value;
            }

            throw new Exception($"unknown token {token}");
        }

        private static HashSet<string> GetTokens(string text)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);

            var i = 0;
            while ((i = text.IndexOf("${", i)) != -1)
            {
                var start = i;
                var end = (int?)null;
                for (; i < text.Length; i++)
                {
                    if (text[i] == '}')
                    {
                        end = i;
                        break;
                    }
                }

                if (end is null)
                {
                    throw new FormatException($"Value '{text}' contains an unclosed replacement token '{text[start..text.Length]}'.");
                }

                var token = text[start..(end.Value + 1)];
                tokens.Add(token);
            }

            return tokens;
        }

        public static string GetScalarValue(YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Scalar)
            {
                throw new TyeYamlException(node.Start,
                    CoreStrings.FormatUnexpectedType(YamlNodeType.Scalar.ToString(), node.NodeType.ToString()));
            }

            return ((YamlScalarNode)node).Value!;
        }

        public static string GetScalarValue(string key, YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Scalar)
            {
                throw new TyeYamlException(node.Start, CoreStrings.FormatExpectedYamlScalar(key));
            }

            return ((YamlScalarNode)node).Value!;
        }

        public static void ThrowIfNotYamlSequence(string key, YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Sequence)
            {
                throw new TyeYamlException(node.Start, CoreStrings.FormatExpectedYamlSequence(key));
            }
        }

        public static void ThrowIfNotYamlMapping(YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Mapping)
            {
                throw new TyeYamlException(node.Start,
                    CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), node.NodeType.ToString()));
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
