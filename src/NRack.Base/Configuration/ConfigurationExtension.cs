﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace NRack.Base.Configuration
{
    /// <summary>
    /// Configuration extension class
    /// </summary>
    public static class ConfigurationExtension
    {
        
        /// Deserializes the specified configuration section.
        /// </summary>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <param name="section">The section.</param>
        /// <param name="reader">The reader.</param>
        public static void Deserialize<TElement>(this TElement section, XmlReader reader)
            where TElement : ConfigurationElement
        {
            if (section is ConfigurationElementCollection)
            {
                var collectionType = section.GetType();
                var att = collectionType.GetCustomAttributes(typeof(ConfigurationCollectionAttribute), true).FirstOrDefault() as ConfigurationCollectionAttribute;

                if (att != null)
                {
                    var property = collectionType.GetProperty("AddElementName", BindingFlags.NonPublic | BindingFlags.Instance);
                    property.SetValue(section, att.AddItemName, null);

                    property = collectionType.GetProperty("RemoveElementName", BindingFlags.NonPublic | BindingFlags.Instance);
                    property.SetValue(section, att.RemoveItemName, null);

                    property = collectionType.GetProperty("ClearElementName", BindingFlags.NonPublic | BindingFlags.Instance);
                    property.SetValue(section, att.ClearItemsName, null);
                }
            }

            var deserializeElementMethod = typeof(TElement).GetMethod("DeserializeElement", BindingFlags.NonPublic | BindingFlags.Instance);
            deserializeElementMethod.Invoke(section, new object[] { reader, false });
        }

        /// <summary>
        /// Gets the child config.
        /// </summary>
        /// <typeparam name="TConfig">The type of the config.</typeparam>
        /// <param name="childElements">The child elements.</param>
        /// <param name="childConfigName">Name of the child config.</param>
        /// <returns></returns>
        public static TConfig GetChildConfig<TConfig>(this NameValueCollection childElements, string childConfigName)
            where TConfig : ConfigurationElement, new()
        {
            var childConfig = childElements.GetValue(childConfigName, string.Empty);

            if (string.IsNullOrEmpty(childConfig))
                return default(TConfig);

            return DeserializeChildConfig<TConfig>(childConfig);
        }


        /// <summary>
        /// Deserializes the child configuration.
        /// </summary>
        /// <typeparam name="TConfig">The type of the configuration.</typeparam>
        /// <param name="childConfig">The child configuration string.</param>
        /// <returns></returns>
        public static TConfig DeserializeChildConfig<TConfig>(string childConfig)
            where TConfig : ConfigurationElement, new()
        {
            // removed extra namespace prefix
            childConfig = childConfig.Replace("xmlns=\"http://schema.supersocket.net/supersocket\"", string.Empty);

            XmlReader reader = new XmlTextReader(new StringReader(childConfig));

            var config = new TConfig();

            reader.Read();
            config.Deserialize(reader);

            return config;
        }

        /// <summary>
        /// Gets the config file path.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        public static string GetConfigFilePath(this ConfigurationElement config)
        {
            var source = config.ElementInformation.Source;

            if (!string.IsNullOrEmpty(source) || !NRackEnv.IsMono)
                return source;

            var configProperty = typeof(ConfigurationElement).GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic);

            if (configProperty == null)
                return string.Empty;

            var configuration = (System.Configuration.Configuration)configProperty.GetValue(config, new object[0]);
            return configuration.FilePath;
        }

        /// <summary>
        /// Gets the current configuration of the configuration element.
        /// </summary>
        /// <returns>The current configuration.</returns>
        /// <param name="configElement">Configuration element.</param>
        public static System.Configuration.Configuration GetCurrentConfiguration(this ConfigurationElement configElement)
        {
            var configElementType = typeof(ConfigurationElement);

            var configProperty = configElementType.GetProperty("CurrentConfiguration", BindingFlags.Instance | BindingFlags.Public);

            if(configProperty == null)
                configProperty = configElementType.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic);

            if (configProperty == null)
                return null;

            return (System.Configuration.Configuration)configProperty.GetValue(configElement, null);
        }

        private static void ResetConfigurationForMono(AppDomain appDomain, string configFilePath)
        {
            appDomain.SetupInformation.ConfigurationFile = configFilePath;

            var configSystem = typeof(ConfigurationManager)
                .GetField("configSystem", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);

            // clear previous state
            typeof(ConfigurationManager)
                .Assembly.GetTypes()
                .Where(x => x.FullName == "System.Configuration.ClientConfigurationSystem")
                .First()
                .GetField("cfg", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(configSystem, null);
        }

        private static void ResetConfigurationForDotNet(AppDomain appDomain, string configFilePath)
        {
            appDomain.SetData("APP_CONFIG_FILE", configFilePath);

            // clear previous state
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

            typeof(ConfigurationManager)
                .GetField("s_initState", flags)
                .SetValue(null, 0);

            typeof(ConfigurationManager)
                .GetField("s_configSystem", flags)
                .SetValue(null, null);

            typeof(ConfigurationManager)
                .Assembly.GetTypes()
                .Where(x => x.FullName == "System.Configuration.ClientConfigPaths")
                .First()
                .GetField("s_current", flags)
                .SetValue(null, null);
        }

        /// <summary>
        /// Reset application's configuration to a another config file
        /// </summary>
        /// <param name="appDomain">the assosiated AppDomain</param>
        /// <param name="configFilePath">the config file path want to reset to</param>
        public static void ResetConfiguration(this AppDomain appDomain, string configFilePath)
        {
            if (NRackEnv.IsMono)
                ResetConfigurationForMono(appDomain, configFilePath);
            else
                ResetConfigurationForDotNet(appDomain, configFilePath);
        }
    }
}
