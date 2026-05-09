Bitsolución Herramientas
Aplicación web desarrollada en ASP.NET Core MVC con soporte para SignalR, Entity Framework Core e Identity, diseñada para centralizar y gestionar herramientas internas como validación de notebooks, cuadraturas, perfilamiento y transmisión de datos en tiempo real.

🚀 Características principales
Arquitectura basada en ASP.NET Core MVC

Comunicación en tiempo real mediante SignalR

Autenticación y manejo de usuarios con Identity

Persistencia de datos con Entity Framework Core

Interfaz basada en Razor Views y Bootstrap

Servicios internos modulares para facilitar mantenimiento y escalabilidad

🧩 Estructura del proyecto
Código
/Areas
   /Identity
/Controllers
/Hubs
/Middleware
/Models
/Services
/Views
wwwroot
⚙️ Requisitos
.NET 8 SDK o superior

Base de datos compatible con Entity Framework Core

Servidor web capaz de ejecutar aplicaciones ASP.NET Core

🛠️ Configuración para desarrollo
1. Restaurar dependencias
bash
dotnet restore
2. Aplicar migraciones
bash
dotnet ef database update
3. Ejecutar la aplicación
bash
dotnet run
La aplicación estará disponible en:

Código
https://localhost:5001
📡 Uso de SignalR
Ejemplo de conexión desde JavaScript:

javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/transmisionHub")
    .build();

connection.start();
🔒 Seguridad
Autenticación integrada con Identity

Roles y políticas configurables

Middleware personalizado para validaciones adicionales

📄 Licencia
Proyecto interno de Bitsolución.
No se permite su distribución sin autorización.
