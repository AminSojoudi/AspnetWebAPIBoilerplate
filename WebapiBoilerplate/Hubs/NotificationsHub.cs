using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebapiBoilerplate.Models;
using Newtonsoft.Json;
using WebapiBoilerplate.Database;

namespace WebapiBoilerplate.Hubs
{
    [HubName("notificationHub")]
    public class NotificationsHub : Hub
    {
        private static IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationsHub>();
        private static DatabaseContext db = new DatabaseContext();

        #region SERVER EVENTS

        /// <summary>
        /// called from server
        /// </summary>
        public static void SayHelloToClients()
        {
            hubContext.Clients.All.onTicketDone("Hello World");
        }

        #endregion



        #region CLIENT EVENTS

        /// <summary>
        /// called from client
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roomId"></param>
        /// <param name="messageText"></param>
        [HubMethodName("ping")]
        public void Ping()
        {
            hubContext.Clients.Client(Context.ConnectionId).pong("HAHAH");
        }

        #endregion

    }

}