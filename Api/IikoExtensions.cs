using MTS_plugin.Utils;
using Resto.Front.Api;
using Resto.Front.Api.Data.Orders;
using System;
using System.Configuration;

namespace MTS_plugin.Api
{
    public static class IikoExtensions
    {
        public static void CompleteOrder(this IDeliveryOrder order)
        {
            var editSession = PluginContext.Operations.CreateEditSession();
            editSession.SetDeliveryDelivered(order);
            var adminPin = ConfigurationManager.AppSettings.Get(Constants.AdminPin);
            var credentials = PluginContext.Operations.AuthenticateByPin(adminPin);
            editSession.ChangeDeliveryActualDeliverTime(DateTime.Now, order);
            PluginContext.Operations.SubmitChanges(credentials, editSession);
            PluginContext.Log.Info($"[IikoExtensions.CompleteOrder]: Delivery {order.Id} in ikko has been successfully marked as delivered");
        }
    }
}