using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ASGK_loyalty_plugin.API;
using ASGK_loyalty_plugin.Views;
using Newtonsoft.Json.Linq;
using Resto.Front.Api;
using Resto.Front.Api.Exceptions;

namespace ASGK_loyalty_plugin.Utils
{
    internal class AdminPin
    {
        private static bool _errorWasShown;
        private static string _value;
        private const string DefaultValue = "1111";

        public static string Value
        {
            get => _value ??= DefaultValue;
            private set => _value = value;
        }

        public static async Task<bool> TryUpdateAsync(RestApi client, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await client.UpdateAdminPinAsync(cancellationToken);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Value = JObject.Parse(response.Content)["admin_pin"]?.ToString();
                        PluginContext.Operations.AuthenticateByPin(Value);
                        PluginContext.Log.Info("[AdminPin.TryUpdateAsync]: Admin pin was successfully updated");
                        return true;
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Unauthorized:
                        OkWindow.Show($"{response.Content}\n Чтобы повторить попытку перепустите Iiko front");
                        return true;
                }

                OkWindow.Show(response.Content);
            }
            catch (Exception e)
            {
                if (!_errorWasShown && e is AuthenticationException)
                {
                    OkWindow.Show(
                        "Введен некорректный пин-код администратора.\n Укажите корректный пин-код в личном кабинете ASGK-GROUP");
                    _errorWasShown = true;
                }

                PluginContext.Log.Error(e.ToString());
            }
            return false;
        }
    }
}
