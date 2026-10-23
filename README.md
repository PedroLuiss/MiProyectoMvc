# MiProyectoMvc

Aplicacion de ejemplo para aprender ASP.NET Core MVC con C#, Razor, Entity Framework
Core, SQL Server y ASP.NET Core Identity. El proyecto apunta a **.NET 10**.

## Como se organiza el codigo

- `Program.cs`: registra servicios y configura el recorrido de cada solicitud HTTP.
- `Controllers`: recibe solicitudes y decide que vista mostrar o que operacion ejecutar.
- `Models`: define los datos que usa la aplicacion, como `Producto`.
- `Data/ApplicationDbContext.cs`: conecta las entidades de la aplicacion con EF Core
  y contiene tambien el esquema de Identity.
- `Repositories`: separa las operaciones de lectura/escritura de la base de datos
  del controlador.
- `Views`: plantillas Razor que mezclan HTML con expresiones C# para presentar datos.
- `Migrations`: instrucciones versionadas que crean o modifican las tablas.
- `wwwroot`: archivos publicos del navegador, como CSS, JavaScript e imagenes.

## Recorrido de una solicitud

1. El navegador solicita una URL, por ejemplo `/Productos`.
2. El enrutamiento configurado en `Program.cs` busca la accion correspondiente.
3. `ProductosController` atiende la solicitud. `[Authorize]` exige iniciar sesion.
4. El controlador llama a `IProductoRepository`; la implementacion consulta EF Core.
5. `ApplicationDbContext` ejecuta la consulta contra SQL Server.
6. El controlador entrega los datos a una vista Razor, que genera el HTML de respuesta.

Para crear un producto, el formulario de `Views/Productos/Create.cshtml` envia un
POST. El controlador valida `ModelState`, agrega la entidad y guarda los cambios;
si la validacion falla, devuelve el formulario para corregirlo.

## Configuracion y archivos generados

- `appsettings.json` contiene valores generales, incluida la cadena de conexion
  local. `appsettings.Development.json` puede sobrescribirlos durante desarrollo.
- `Properties/launchSettings.json` configura los perfiles locales de ejecucion.
- Estos archivos usan formato JSON, por eso se explican aqui en vez de insertar
  comentarios que puedan invalidar la configuracion.
- `Migrations/*.Designer.cs` y `ApplicationDbContextModelSnapshot.cs` los genera
  EF Core; se actualizan con las herramientas de migracion, no manualmente.
- `wwwroot/lib` contiene dependencias de terceros; no forma parte del codigo propio.

## Ejecutar localmente

1. Instala el SDK de .NET 10 y SQL Server o SQL Server Express.
2. Ajusta `ConnectionStrings:DefaultConnection` en `appsettings.json` para tu servidor.
3. Instala la herramienta de EF Core si aun no esta disponible:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.13
   ```

4. Desde la carpeta del proyecto, restaura paquetes y crea/aplica la base de datos:

   ```powershell
   dotnet restore
   dotnet ef database update
   ```

5. Inicia la aplicacion:

   ```powershell
   dotnet run
   ```

El perfil local usa las URL definidas en `Properties/launchSettings.json`.

## Crear una migracion

Despues de cambiar una entidad o el contexto, genera una migracion y aplicala:

```powershell
dotnet ef migrations add NombreDescriptivo
dotnet ef database update
```

Las migraciones describen cambios incrementales al esquema; no reemplazan una
estrategia de respaldo para datos importantes.
