using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;
using Newtonsoft.Json.Linq;

namespace Micrsosoft.AspNetCore.Mvc
{
    /// <summary>
    /// Various extension methods for the ASP.NET CORE MVC <see cref="Controller"/> class.
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="Controller.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The MVC <see cref="Controller"/> instance.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static Task<int> NotifyAsync(this Controller controller, string action, object data)
        {
            var notifications = new IWebHookNotification[] { ControllerExtensions.CreateNotification(action, data) };
            return NotifyAsync(controller, notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="Controller.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The MVC <see cref="Controller"/> instance.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static Task<int> NotifyAsync(this Controller controller, string action, object data, Func<WebHook, string, bool> predicate)
        {
            var notifications = new IWebHookNotification[] { ControllerExtensions.CreateNotification(action, data) };
            return NotifyAsync(controller, notifications, predicate);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="Controller.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The MVC <see cref="Controller"/> instance.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static Task<int> NotifyAsync(this Controller controller, params IWebHookNotification[] notifications)
        {
            return NotifyAsync(controller, notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="Controller.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The MVC <see cref="Controller"/> instance.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static async Task<int> NotifyAsync(this Controller controller, IEnumerable<IWebHookNotification> notifications, Func<WebHook, string, bool> predicate)
        {
            if (controller == null)
            {
                throw new ArgumentNullException("controller");
            }
            if (notifications == null)
            {
                throw new ArgumentNullException("notifications");
            }
            if (!notifications.Any())
            {
                return 0;
            }

            // Get the User ID from the User principal
            string userId = controller.User.Identity.Name;

            // Send a notification to registered WebHooks with matching filters
            IWebHookManager manager = (IWebHookManager) controller.HttpContext.RequestServices.GetService(typeof(IWebHookManager));
            return await manager.NotifyAsync(userId, notifications, predicate);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The <see cref="Controller"/> instance.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static Task<int> NotifyAllAsync(this Controller controller, string action, object data)
        {
            var notifications = new IWebHookNotification[] { ControllerExtensions.CreateNotification(action, data) };
            return NotifyAllAsync(controller, notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The <see cref="Controller"/> instance.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static Task<int> NotifyAllAsync(this Controller controller, string action, object data, Func<WebHook, string, bool> predicate)
        {
            var notifications = new IWebHookNotification[] { ControllerExtensions.CreateNotification(action, data) };
            return NotifyAllAsync(controller, notifications, predicate);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The <see cref="Controller"/> instance.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static Task<int> NotifyAllAsync(this Controller controller, params IWebHookNotification[] notifications)
        {
            return NotifyAllAsync(controller, notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="controller">The <see cref="Controller"/> instance.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public static async Task<int> NotifyAllAsync(this Controller controller, IEnumerable<IWebHookNotification> notifications, Func<WebHook, string, bool> predicate)
        {
            if (controller == null)
            {
                throw new ArgumentNullException("controller");
            }
            if (notifications == null)
            {
                throw new ArgumentNullException("notifications");
            }
            if (!notifications.Any())
            {
                return 0;
            }

            // Send a notification to registered WebHooks across all users with matching filters
            IWebHookManager manager = (IWebHookManager)controller.HttpContext.RequestServices.GetService(typeof(IWebHookManager));
            return await manager.NotifyAllAsync(notifications, predicate);
        }

        private static IWebHookNotification CreateNotification(string action, object data)
        {
            dynamic notification = new System.Dynamic.ExpandoObject();
            notification.Action = action;
            return notification;
        }
    }
}
