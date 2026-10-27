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
  del controlador. Los repositorios preparan cambios, pero no los guardan por separado.
- `Repositories/IUnitOfWork.cs` y `UnitOfWork.cs`: coordinan los repositorios de
  productos y categorías y confirman sus cambios juntos con `SaveAsync()`.
- `Views`: plantillas Razor que mezclan HTML con expresiones C# para presentar datos.
- `Migrations`: instrucciones versionadas que crean o modifican las tablas.
- `wwwroot`: archivos publicos del navegador, como CSS, JavaScript e imagenes.
- `wwwroot/css/input.css`: punto de entrada de Tailwind (tokens y componentes).
- `package.json`: scripts de npm que compilan el CSS.

## Recorrido de una solicitud

1. El navegador solicita una URL, por ejemplo `/Productos`.
2. El enrutamiento configurado en `Program.cs` busca la acción correspondiente.
3. `ProductosController` atiende la solicitud. `[Authorize]` exige iniciar sesión.
4. El controlador obtiene los repositorios desde `IUnitOfWork`; estos consultan EF
   Core o preparan cambios en el contexto compartido.
5. Al terminar la operación, el controlador llama a `SaveAsync()` una sola vez para
   confirmar los cambios pendientes.
6. `ApplicationDbContext` ejecuta la operación contra SQL Server y el controlador
   entrega los datos a una vista Razor, que genera el HTML de respuesta.

### Unidad de trabajo y repositorios

`IUnitOfWork` expone `Productos` (`IProductoRepository`) y `Categorias`
(`ICategoriaRepository`), además del método general `SaveAsync()`. Ambas
implementaciones usan la misma instancia de `ApplicationDbContext` durante la
solicitud HTTP. Por eso se pueden preparar cambios con más de un repositorio y
confirmarlos en conjunto:

```csharp
await unitOfWork.Productos.AddAsync(producto);
await unitOfWork.Categorias.UpdateAsync(categoria);
await unitOfWork.SaveAsync();
```

`SaveAsync()` delega a `ApplicationDbContext.SaveChangesAsync()`, que confirma los
cambios rastreados por EF Core en una única operación. Los métodos de escritura de
los repositorios no llaman directamente a `SaveChangesAsync()`. Para incorporar otro
repositorio, se agrega su interfaz como propiedad de `IUnitOfWork`, se inyecta su
implementación en `UnitOfWork` y se registra en `Program.cs` con ciclo de vida
`Scoped`, igual que el contexto.

Para crear o editar un producto, `Views/Productos/Create.cshtml` y
`Views/Productos/Edit.cshtml` reutilizan el formulario parcial
`Views/Productos/_Form.cshtml`. El navegador envía un POST con los campos y, si se
seleccionó una foto, con el archivo en formato multipart. El controlador valida los
datos, la categoría y que el SKU no esté repetido. Si hay errores, vuelve a mostrar
el formulario; si todo es válido, guarda los cambios y redirige al catálogo.

### Acciones AJAX y vistas parciales

En el catálogo, `wwwroot/js/product-actions.js` intercepta la eliminación de
productos y la actualización del stock y las envía con `fetch`. Las solicitudes
incluyen el token antifalsificación y el encabezado `X-Requested-With`.
`ProductosController` detecta esas solicitudes y devuelve HTML renderizado por
parciales Razor:

- `_ProductoResults.cshtml` vuelve a dibujar el contador y la lista después de
  eliminar un producto, incluidos los resultados vacíos y los filtros activos.
- `_ProductoStock.cshtml` reemplaza el control del stock después de guardarlo y
  muestra errores de validación sin recargar la página.

Las mismas acciones conservan el envío de formularios tradicional como alternativa
si JavaScript está deshabilitado. Los formularios de escritura usan
`@Html.AntiForgeryToken()` también cuando se envían por AJAX.

## Categorías, campos y fotos de productos

La sección **Categorías** permite crear, editar y eliminar categorías. Cada producto
debe pertenecer a una categoría existente. La categoría **General** es la opción
predeterminada y no se puede eliminar; tampoco se permite eliminar una categoría
mientras tenga productos asociados.

Los productos incluyen nombre, descripción, SKU único, marca, precio, existencias,
estado activo/inactivo, fecha de alta y una foto opcional. El catálogo permite filtrar
por categoría y buscar por nombre, SKU o marca. Desde el detalle se puede consultar
la información y acceder a las acciones para editar o eliminar.

Las imágenes admitidas son JPG, PNG y WebP, con un máximo de 2 MB. El servidor
comprueba la extensión, el tipo declarado y la firma del contenido; guarda cada
archivo con un nombre aleatorio en `wwwroot/uploads/productos`. Al reemplazar o
eliminar un producto, también se elimina su foto anterior. La carpeta de cargas debe
tener permisos de escritura para la identidad que ejecuta la aplicación. La vista
previa del formulario se muestra localmente antes de enviar la imagen.

Las solicitudes y formularios usan la cultura `es-MX`. El precio es un campo decimal
numérico y sus límites de validación se interpretan con cultura invariable; esto evita
errores al mostrar el formulario o validar el precio según la configuración regional.

### Actualizar la base de datos

La migración `AddCategoriesAndProductFields` agrega categorías y los campos nuevos
del producto. Para aplicarla a una base que solo tenga la migración inicial, ejecuta:

```powershell
dotnet ef database update
```

La migración conserva los productos existentes: los asigna a **General**, les genera
un SKU `LEGACY-<Id>`, los marca como activos y establece su stock inicial en cero.
La fecha de alta se completa con la fecha UTC de la migración. Los cambios posteriores
al modelo deben guardarse en una nueva migración; no se edita manualmente el snapshot
de EF Core.

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

## Configuración y archivos generados

- `appsettings.json` contiene valores generales, incluida la cadena de conexion
  local. `appsettings.Development.json` puede sobrescribirlos durante desarrollo.
- `Properties/launchSettings.json` configura los perfiles locales de ejecución.
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
