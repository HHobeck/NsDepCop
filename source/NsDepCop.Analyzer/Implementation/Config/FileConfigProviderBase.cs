﻿using System;
using System.IO;
using Codartis.NsDepCop.Core.Interface.Config;
using Codartis.NsDepCop.Core.Util;

namespace Codartis.NsDepCop.Core.Implementation.Config
{
    /// <summary>
    /// Abstract base class for file based config implementations.
    /// Reloads only if the file changed.
    /// </summary>
    /// <remarks>
    /// Base class ensures that all operations are executed in an atomic way so no extra locking needed.
    /// </remarks>
    public abstract class FileConfigProviderBase : ConfigProviderBase
    {
        private bool _configFileExists;
        private DateTime _configLastLoadUtc;
        private ConfigLoadResult _lastConfigLoadResult;

        public string ConfigFilePath { get; }

        protected FileConfigProviderBase(string configFilePath, MessageHandler traceMessageHandler)
            : base(traceMessageHandler)
        {
            ConfigFilePath = configFilePath;
        }

        public override string ConfigLocation => ConfigFilePath;

        public int InheritanceDepth => ConfigBuilder?.InheritanceDepth ?? ConfigDefaults.InheritanceDepth;

        public bool HasConfigFileChanged()
        {
            return ConfigFileCreatedOrDeleted()
                || ConfigFileModifiedSinceLastLoad();
        }

        protected override ConfigLoadResult LoadConfigCore()
        {
            _lastConfigLoadResult = LoadConfigFromFile();
            return _lastConfigLoadResult;
        }

        protected override ConfigLoadResult RefreshConfigCore()
        {
            if (!HasConfigFileChanged())
                return _lastConfigLoadResult;

            LogTraceMessage($"Refreshing config {this}.");
            return LoadConfigCore();
        }

        private ConfigLoadResult LoadConfigFromFile()
        {
            try
            {
                _configFileExists = File.Exists(ConfigFilePath);
                if (!_configFileExists)
                    return ConfigLoadResult.CreateWithNoConfig();

                _configLastLoadUtc = DateTime.UtcNow;

                var configBuilder = CreateConfigBuilderFromFile(ConfigFilePath)
                     .SetDefaultInfoImportance(DefaultInfoImportance)
                     .MakePathsRooted(Path.GetDirectoryName(ConfigFilePath));

                return ConfigLoadResult.CreateWithConfig(configBuilder);
            }
            catch (Exception e)
            {
                LogTraceMessage($"BuildConfig exception: {e}");
                return ConfigLoadResult.CreateWithError(e);
            }
        }

        protected abstract AnalyzerConfigBuilder CreateConfigBuilderFromFile(string configFilePath);

        private bool ConfigFileCreatedOrDeleted()
        {
            return _configFileExists != File.Exists(ConfigFilePath);
        }

        private bool ConfigFileModifiedSinceLastLoad()
        {
            return File.Exists(ConfigFilePath)
                && _configLastLoadUtc < File.GetLastWriteTimeUtc(ConfigFilePath);
        }

        private void LogTraceMessage(string message) => TraceMessageHandler?.Invoke(message);
    }
}
