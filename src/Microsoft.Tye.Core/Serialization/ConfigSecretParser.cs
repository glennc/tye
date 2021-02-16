using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Tye.ConfigModel;
using YamlDotNet.RepresentationModel;

namespace Tye.Serialization
{
    public static class ConfigSecretParser
    {
        public static void HandleSecrets(YamlSequenceNode yamlSequenceNode, List<ConfigSecret> secrets)
        {
            foreach (var child in yamlSequenceNode.Children)
            {
                YamlParser.ThrowIfNotYamlMapping(child);
                var secret = new ConfigSecret();
                HandleSecretsMapping((YamlMappingNode)child, secret);
                secrets.Add(secret);
            }
        }

        private static void HandleSecretsMapping(YamlMappingNode yamlMappingNode, ConfigSecret secret)
        {
            foreach (var child in yamlMappingNode!.Children)
            {
                var key = YamlParser.GetScalarValue(child.Key);

                switch (key)
                {
                    case "name":
                        secret.Name = YamlParser.GetScalarValue(key, child.Value).ToLowerInvariant();
                        break;
                    case "source":
                        secret.Source = YamlParser.GetScalarValue(key, child.Value).ToLowerInvariant();
                        break;
                    case "type":
                        secret.Type = YamlParser.GetScalarValue(key, child.Value).ToLowerInvariant();
                        break;
                    default:
                        throw new TyeYamlException(child.Key.Start, CoreStrings.FormatUnrecognizedKey(key));
                }
            }
        }
    }
}
