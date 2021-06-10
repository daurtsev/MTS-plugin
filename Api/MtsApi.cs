using MTS_plugin.Database;
using MTS_plugin.MTSb2b;
using MTS_plugin.MTSLogin;
using MTS_plugin.MTSTask;
using MTS_plugin.Requests;
using MTS_plugin.Utils;
using Resto.Front.Api;
using Resto.Front.Api.Data.Orders;
using System;
using System.Configuration;
using System.Linq;
using System.ServiceModel;

namespace MTS_plugin.Api
{
    public class MtsApi : MtsRequestObj
    {
        private static readonly BasicHttpsBinding binding = new BasicHttpsBinding();
        private static readonly EndpointAddress endpointLogin = new EndpointAddress("https://api.mpoisk.ru/mts/ws/me_b2b_54/security.asmx");
        private static readonly EndpointAddress endpointTask = new EndpointAddress("https://api.mpoisk.ru/mts/ws/me_b2b_54/task_api.asmx");
        private static readonly EndpointAddress endpointB2b = new EndpointAddress("https://api.mpoisk.ru/mts/ws/me_b2b_54/b2b_api.asmx");

        private readonly SecuritySoap _auto = new SecuritySoapClient(binding, endpointLogin);
        private readonly task_apiSoap _delivery = new task_apiSoapClient(binding, endpointTask);
        private readonly b2b_apiSoap _b2b = new b2b_apiSoapClient(binding, endpointB2b);

        private readonly LocalStorage _db = new LocalStorage("");

        public void Login()
        {
            var login = ConfigurationManager.AppSettings.Get(Constants.Login);
            var password = ConfigurationManager.AppSettings.Get(Constants.Password);
            try
            {
                var user = _auto.Login(login, password, new PlatformInfo());
                Plugin.Token = user.Token;
                PluginContext.Log.Info("[MtsApi.Login]: Login is successfully completed");
            }
            catch (Exception e)
            {
                PluginContext.Log.Info("[MtsApi.Login]: Error login to MTS service \n" +
                                       $"{e.Message}");
            }
        }

        public void Logout()
        {
            var request = new LogoutRequest { SecurityHeader = new MTSLogin.SecurityHeader { SessionToken = Plugin.Token } };
            _auto.LogoutAsync(request);
            PluginContext.Log.Info("[MtsApi.Logout]: Logout is successfully completed");
        }

        public bool CheckCourierMts(IDeliveryOrder order)
        {
            var courierIdString = order.Courier.Card; //поле с id мтс
            if (!long.TryParse(courierIdString, out var courierId))
            {
                PluginContext.Log.Info($"[MtsApi.CheckCourier]: Wrong MTS courier id {courierIdString} in iiko");
                return false;
            }
            var request = new GetSubscriberRequest
            {
                SecurityHeader = new MTSb2b.SecurityHeader { SessionToken = Plugin.Token },
                id = courierId,
            };
            try
            {
                _b2b.GetSubscriber(request);
                PluginContext.Log.Info($"[MtsApi.CheckCourier]: MTS Courier with id {courierId} found");
            }
            catch (Exception e)
            {
                PluginContext.Log.Info($"[MtsApi.CheckCourier]: MTS Courier with id {courierId} not found");
            }
            return true;
        }

        public bool SetMtsTask(IDeliveryOrder order, bool isCourier)
        {
            var courierIdString = order.Courier.Card; //поле с id мтс
            long.TryParse(courierIdString, out var courierId);

            var street = order.Address.Street.Name;
            var building = order.Address.Building;
            var entrance = order.Address.Entrance ?? "-";
            var floor = order.Address.Floor ?? "-";

            var fullAddress = "Полный адрес: ул. " + street + " д. " + building + " под. " + entrance +
                              " этаж " + floor + " кв./офис " + order.Address.Flat;

            var comment = order.Comment;
            if (comment == "") comment = "Отсутствует";

            var addressComment = order.Address.AdditionalInfo;
            if (addressComment == "примечание") addressComment = "Отсутствует";

            var request = new SaveTaskRequest
            {
                SecurityHeader = new MTSTask.SecurityHeader() { SessionToken = Plugin.Token },
                taskArg = new TaskSaveModel
                {
                    SubscriberID = isCourier ? (long?)courierId : null,
                    Title = "Заказ № " + order.Number,
                    Description = fullAddress +
                                  " Комментарий к адресу: " + addressComment +
                                  " Имя Отчество клиента: " + order.Client.Name +
                                  " Телефон: " + order.Phone +
                                  " Комментарий к заказу: " + comment,
                    Address = order.Address.Street.City.Name + ", улица " + street + ", " + building,
                    ClientName = order.Client.Name,
                    ClientPhone = order.Phone,
                    Priority = 1,
                    StartDate = order.SendTime ?? DateTime.Now,
                    Status = 4
                }
            };
            try
            {
                var mtsTask = _delivery.SaveTask(request);
                var obj = new IdObj
                {
                    IikoId = order.Id,
                    MtsId = mtsTask.SaveTaskResult,
                    Status = order.DeliveryStatus,
                };
                _db.Add<IdObj>(obj);
                PluginContext.Log.Info($"[MtsApi.SetMtsTask]: MTS task {mtsTask.SaveTaskResult} successfully created");
            }
            catch (Exception e)
            {
                PluginContext.Log.Info("[MtsApi.SetMtsTask]: Task creation failed");
                return false;
            }
            return true;
        }

        public bool ReadinessCheck(IDeliveryOrder order)
        {
            long taskId;
            try
            {
                var r = _db.Get<IdObj>(x => x.IikoId == order.Id).ToArray();
                taskId = r[0].MtsId;
            }
            catch (Exception e)
            {
                PluginContext.Log.Info($"[MtsApi.ReadinessCheck]: Order {order.Id} not found in the local database");
                return false;
            }
            
            var request = new GetTaskCommentsRequest
            {
                SecurityHeader = new MTSTask.SecurityHeader { SessionToken = Plugin.Token },
                taskID = taskId
            };

            TaskComment[] taskCommentsResult;
            try
            {
                taskCommentsResult = _delivery.GetTaskComments(request).GetTaskCommentsResult;
            }
            catch (Exception e)
            {
                PluginContext.Log.Info($"[MtsApi.ReadinessCheck]: MTS task {taskId} not found");
                return false;
            }

            if (taskCommentsResult[0].NewStatus == 2)
            {
                PluginContext.Log.Info($"[MtsApi.ReadinessCheck]: MTS task {taskId} has been successfully marked as rejected");
                _db.Delete<IdObj>(order.Id);
                return true;
            }
            if (taskCommentsResult[0].NewStatus != 3)
            {
                return false;
            }

            PluginContext.Log.Info($"[MtsApi.ReadinessCheck]: MTS task {taskId} has been successfully marked as completed");
            order.CompleteOrder();
            return true;
        }
    }
}
