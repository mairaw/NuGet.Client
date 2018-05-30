// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NuGet.PackageManagement.VisualStudio
{
    internal class SettingsStoreWrapper : ISettingsStore
    {
        private readonly IVsSettingsStore _store;

        public SettingsStoreWrapper(IVsSettingsStore store)
        {
            _store = store;
        }

        public bool CollectionExists(string collection)
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                int exists;
                var hr = _store.CollectionExists(collection, out exists);
                return ErrorHandler.Succeeded(hr) && exists == 1;
            });
        }

        public bool GetBoolean(string collection, string propertyName, bool defaultValue)
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                int value;
                _store.GetBoolOrDefault(collection, propertyName, defaultValue ? 1 : 0, out value);
                return value != 0;
            });
        }

        public int GetInt32(string collection, string propertyName, int defaultValue)
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                int value;
                var hr = _store.GetIntOrDefault(collection, propertyName, defaultValue, out value);
                return ErrorHandler.Succeeded(hr) ? value : 0;
            });
        }

        public string GetString(string collection, string propertyName, string defaultValue)
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                string value;
                var hr = _store.GetStringOrDefault(collection, propertyName, defaultValue, out value);
                return ErrorHandler.Succeeded(hr) ? value : null;
            });
        }
    }
}
