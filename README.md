# Bitsolución Herramientas

Plataforma interna desarrollada en **ASP.NET Core MVC**, diseñada para centralizar y gestionar herramientas operativas como validación de notebooks, cuadraturas, perfilamiento y transmisión de datos en tiempo real.

---

## ✨ Características principales

- Arquitectura basada en **ASP.NET Core MVC**
- Comunicación en tiempo real con **SignalR**
- Autenticación y manejo de usuarios mediante **Identity**
- Persistencia con **Entity Framework Core**
- Interfaz con **Razor Views** y **Bootstrap**
- Servicios internos modulares para facilitar mantenimiento y escalabilidad

---

## 🧩 Estructura del proyecto

/Areas
/Identity
/Controllers
/Hubs
/Middleware
/Models
/Services
/Views
/wwwroot


---

## 🚀 Requisitos

- .NET 8 SDK o superior  
- Base de datos compatible con Entity Framework Core  
- Servidor web capaz de ejecutar aplicaciones ASP.NET Core  

---

## 🛠️ Configuración para desarrollo

### 1. Restaurar dependencias
dotnet restore


### 2. Aplicar migraciones
dotnet ef database update


### 3. Ejecutar la aplicación
dotnet run


La aplicación estará disponible en:

https://localhost:5001


---

## 📡 Uso de SignalR

Ejemplo de conexión desde JavaScript:

const connection = new signalR.HubConnectionBuilder()
.withUrl("/transmisionHub")
.build();

connection.start();


---

## 🔒 Seguridad

- Autenticación integrada con Identity  
- Roles y políticas configurables  
- Middleware personalizado para validaciones adicionales  

---

## 📄 Licencia

Proyecto interno de Bitsolución.  
No se permite su distribución sin autorización.
