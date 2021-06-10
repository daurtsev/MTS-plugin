using MTS_plugin.Api;
using MTS_plugin.Observers;
using MTS_plugin.Requests;
using Resto.Front.Api;
using Resto.Front.Api.Attributes;
using System;

namespace MTS_plugin
{
    [PluginLicenseModuleId(21016318)]
    public class Plugin : IFrontPlugin
    {
        public static string Token { get; set; }
        private readonly IDisposable _deliveryObserver;
        private readonly MtsApi _mts = new MtsApi();
        private readonly MtsTask _mtsTask = new MtsTask();
        public Plugin()
        {
            PluginContext.Log.Info("MTS-Plugin started");
            _mts.Login();
            _mtsTask.OldMtsTasks();
            _deliveryObserver = PluginContext.Notifications.DeliveryOrderChanged.Subscribe(new DeliveryOrderObserver());
        }

        public void Dispose()
        {
            _mts.Logout();
            _deliveryObserver?.Dispose();
            RequestQueue.Instance.Dispose();
            PluginContext.Log.Info("MTS-Plugin stopped");
        }
    }
}
