using System;
using System.Linq;
using MTS_plugin.Database;
using Resto.Front.Api;
using Resto.Front.Api.Data.Brd;
using Resto.Front.Api.Data.Orders;

namespace MTS_plugin.Api
{
    internal class MtsTask
    {
        private readonly LocalStorage _db = new LocalStorage("");
        private readonly MtsApiTasks _mtsApiTasks = new MtsApiTasks();

        public async void NewMtsTask(IDeliveryOrder order)
        {
            try
            {
                var r = _db.Get<IdObj>(x => x.IikoId == order.Id).ToArray();
                if (r[0].Status == DeliveryStatus.OnWay) return;
            }
            catch (Exception e)
            {
            }

            var isCourier = await _mtsApiTasks.CheckCourierMtsTask(order);
            await _mtsApiTasks.SetMtsTaskTask(order, isCourier);
            await _mtsApiTasks.ReadinessCheckTask(order);
        }

        public void OldMtsTasks()
        {
            IdObj[] r;
            try
            {
                r = _db.Get<IdObj>().ToArray();
            }
            catch (Exception e)
            {
                return;
            }

            if (r.Length == 0)
            {
                PluginContext.Log.Info("[MtsTask.OldMtsTask]: No old orders found in the local database");
                return;
            }

            foreach (var t in r)
            {
                IDeliveryOrder order;
                try
                {
                    order = PluginContext.Operations.GetDeliveryOrderById(t.IikoId);
                }
                catch (Exception e)
                {
                    PluginContext.Log.Info($"[MtsTask.OldMtsTask]: No such order {t.IikoId} found");
                    throw;
                }

                try
                {
                    _mtsApiTasks.ReadinessCheckTask(order);
                }
                catch (Exception e)
                {
                    PluginContext.Log.Info(
                        $"[MtsTask.OldMtsTask]: it's not possible to get information on the order {t.IikoId} from the MTS server");
                    throw;
                }
            }
        }
    }
}