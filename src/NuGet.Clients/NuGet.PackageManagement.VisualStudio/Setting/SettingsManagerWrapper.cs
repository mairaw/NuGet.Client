// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio;

namespace NuGet.PackageManagement.VisualStudio
{
    internal class SettingsManagerWrapper : ISettingsManager
    {
        private readonly AsyncLazy<IVsSettingsManager> _settingsManager;

        public SettingsManagerWrapper(IServiceProvider serviceProvider)
        {
            _settingsManager = new AsyncLazy<IVsSettingsManager>(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                return (IVsSettingsManager)serviceProvider.GetService(typeof(SVsSettingsManager));
            }, NuGetUIThreadHelper.JoinableTaskFactory);
        }

        public ISettingsStore GetReadOnlySettingsStore()
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                IVsSettingsStore settingsStore;
                var hr = (await _settingsManager.GetValueAsync()).GetReadOnlySettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out settingsStore);
                if (ErrorHandler.Succeeded(hr)
                    && settingsStore != null)
                {
                    return new SettingsStoreWrapper(settingsStore);
                }

                return null;
            });
        }

        public IWritableSettingsStore GetWritableSettingsStore()
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                IVsWritableSettingsStore settingsStore;
                var hr = (await _settingsManager.GetValueAsync()).GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out settingsStore);
                if (ErrorHandler.Succeeded(hr)
                    && settingsStore != null)
                {
                    return new WritableSettingsStoreWrapper(settingsStore);
                }

                return null;
            });
        }
    }
}
