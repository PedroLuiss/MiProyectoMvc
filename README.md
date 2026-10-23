# MiProyectoMvc

Aplicacion de ejemplo para aprender ASP.NET Core MVC con C#, Razor, Entity Framework
Core, SQL Server, ASP.NET Core Identity y Tailwind CSS. El proyecto apunta a **.NET 10**.

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
- `wwwroot/css/input.css`: punto de entrada de Tailwind (tokens y componentes).
- `package.json`: scripts de npm que compilan el CSS.

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

## Estilos con Tailwind CSS

El proyecto usa **Tailwind CSS 4** a traves del CLI oficial de npm. La configuracion
es *CSS-first*: no existe `tailwind.config.js`; los tokens viven en `@theme` dentro
de `wwwroot/css/input.css`.

- `@source "../../Views"` y `@source "../../wwwroot/js"` indican donde buscar clases.
  Sin esas lineas Tailwind no ve las vistas Razor, que estan fuera de `wwwroot`.
- `@theme` define la paleta `brand-*` y la tipografia.
- `@layer components` contiene solo las primitivas repetidas: `.btn-primary`,
  `.btn-secondary`, `.input-field` y los estilos de validacion de ASP.NET Core.
- Las clases se escriben completas en el HTML. Tailwind no detecta nombres
  construidos dinamicamente como `"text-" + color`; en ese caso usa un `switch`
  que devuelva la clase entera.
- Bootstrap se retiro de las vistas propias. Las paginas de Identity conservan el
  Bootstrap que trae su paquete para no mezclar dos sistemas en el mismo layout.

Comandos disponibles:

```powershell
npm ci              # instala dependencias (una vez, o al cambiar package-lock.json)
npm run css:dev     # recompila app.css al guardar cambios, sin minificar
npm run css:build   # compila app.css minificado para revisar o publicar
```

`wwwroot/css/app.css` es un archivo generado y no se versiona. `dotnet publish`
lo compila automaticamente mediante el target `TailwindPublish` del `.csproj`.

## Configuracion y archivos generados

- `appsettings.json` contiene valores generales, incluida la cadena de conexion
  local. `appsettings.Development.json` puede sobrescribirlos durante desarrollo.
- `Properties/launchSettings.json` configura los perfiles locales de ejecucion.
- Estos archivos usan formato JSON, por eso se explican aqui en vez de insertar
  comentarios que puedan invalidar la configuracion.
- `wwwroot/css/app.css` lo genera Tailwind; no se edita a mano.
- `Migrations/*.Designer.cs` y `ApplicationDbContextModelSnapshot.cs` los genera
  EF Core; se actualizan con las herramientas de migracion, no manualmente.
- `wwwroot/lib` contiene dependencias de terceros; no forma parte del codigo propio.
  Se conservan jQuery y jQuery Validation porque la validacion en cliente las usa.
- `node_modules` y `package-lock.json`: dependencias de npm. El lock se versiona
  para que `npm ci` instale exactamente las mismas versiones.

## Ejecutar localmente

1. Instala el SDK de .NET 10 y SQL Server o SQL Server Express.
2. Instala Node.js 20 o superior (incluye npm).
3. Ajusta `ConnectionStrings:DefaultConnection` en `appsettings.json` para tu servidor.
4. Instala las dependencias de front-end:

   ```powershell
   npm ci
   ```

5. Instala la herramienta de EF Core si aun no esta disponible:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.13
   ```

6. Desde la carpeta del proyecto, restaura paquetes y crea/aplica la base de datos:

   ```powershell
   dotnet restore
   dotnet ef database update
   ```

7. Compila el CSS y arranca la aplicacion. Usa dos terminales: una para Tailwind en
   modo watch y otra para el servidor.

   ```powershell
   # Terminal 1
   npm run css:dev

   # Terminal 2
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
