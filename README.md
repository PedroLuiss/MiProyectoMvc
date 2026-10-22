# 🚀 Sistema MVC con ASP.NET Core & Identity

Aplicación web desarrollada en **.NET 8/10** bajo el patrón arquitectónico **MVC (Model-View-Controller)**, implementando **Patrón Repositorio**, **Inyección de Dependencias** y un sistema seguro de autenticación con **ASP.NET Core Identity**.

## 🛠️ Tecnologías y Características
- **Backend:** C#, ASP.NET Core MVC.
- **Base de Datos:** SQL Server (LocalDB / SQLEXPRESS) con **Entity Framework Core (Code First)**.
- **Arquitectura & Patrones:** 
  - Patrón Repositorio (`IProductoRepository` y `ProductoRepository`).
  - Inyección de Dependencias (DI) configurada en el pipeline.
- **Seguridad:** Autenticación y gestión de usuarios con **ASP.NET Core Identity** (rutas protegidas con `[Authorize]`).

## 📦 Guía de Instalación y Ejecución local

Sigue estos pasos para clonar y ejecutar el proyecto en tu máquina:

1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/TuUsuarioDeGithub/NombreDeTuRepositorio.git
   cd MiProyectoMvc