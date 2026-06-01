using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotebookValidator.Web.Hubs
{
    [Authorize]
    public class NotificacionesHub : Hub
    {
        // El cliente se une a su grupo personal al conectar
        // Esto permite enviar notificaciones a un usuario específico
        // aunque tenga múltiples pestañas abiertas
        public override async Task OnConnectedAsync()
        {
            string userId = Context.UserIdentifier ?? "";
            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            await base.OnConnectedAsync();
        }
    }
}
