using MTS_plugin.Api;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;
using System;

namespace MTS_plugin.Observers
{
    internal class DeliveryOrderObserver : IObserver<EntityChangedEventArgs<IDeliveryOrder>>
    {
        private readonly MtsTask _mtsTask = new MtsTask();
        public void OnNext(EntityChangedEventArgs<IDeliveryOrder> value)
        {
            var order = value.Entity;
            if (value.EventType == EntityEventType.Updated &&
                value.Entity.DeliveryStatus == Resto.Front.Api.Data.Brd.DeliveryStatus.OnWay &&
                value.Entity.CancelCause == null &&
                value.Entity.CancelComment == null)
            {
                _mtsTask.NewMtsTask(order);
            }
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception e)
        {

        }
    }
}