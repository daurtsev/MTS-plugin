using System;
using System.Threading.Tasks;
using MTS_plugin.Requests;
using Resto.Front.Api.Data.Orders;

namespace MTS_plugin.Api
{
    class MtsApiTasks
    {
        private readonly MtsApi _mts = new MtsApi();

        public async Task<bool> CheckCourierMtsTask(IDeliveryOrder order)
        {
            var courierRequest = new MtsRequestObj
            {
                Task = new Task<bool>(() => _mts.CheckCourierMts(order)),
                Priority = Priority.Normal
            };
            RequestQueue.Instance.Add(courierRequest);
            return await courierRequest.Task;
        }

        public async Task SetMtsTaskTask(IDeliveryOrder order, bool isCourier)
        {
            var successTask = false;
            do
            {
                var taskRequest = new MtsRequestObj
                {
                    Task = new Task<bool>(() => _mts.SetMtsTask(order, isCourier)),
                    Priority = Priority.Normal
                };
                RequestQueue.Instance.Add(taskRequest);
                successTask = await taskRequest.Task;
            } while (!successTask);
        }

        public async Task ReadinessCheckTask(IDeliveryOrder order)
        {
            var successCheck = false;
            do
            {
                var checkRequest = new MtsRequestObj
                {
                    Task = new Task<bool>(() => _mts.ReadinessCheck(order)),
                    Priority = Priority.Low
                };
                RequestQueue.Instance.Add(checkRequest);
                successCheck = await checkRequest.Task;
                if (checkRequest.Task.IsFaulted) break;
            } while (!successCheck);
        }
    }
}
