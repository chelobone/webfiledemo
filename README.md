# Laboratorio carga de archivos: Backend

### Este es un laboratorio para entender como se comportan las distintas formas de transferir archivos del lado del backend.

## *Tus*

#### Es un estándar de transferencia de archivos. Este framework separa en bloques de bytes el archivo, y los envia al servidor para que este los junte. Pueden revisar esta [prueba de concepto](https://github.com/chelobone/chunkUpload) que realicé el año 2019, para entender como funciona este tipo de estándar

El archivo [Program.cs](Program.cs) manejar los eventos asociados al estándar Tus

- OnBeforeCreateAsync
- OnCreateCompleteAsync
- OnBeforeDeleteAsync
- OnDeleteCompleteAsync
- OnFileCompleteAsync

Para funcionar mediante este estándar, se debe configurar el CORS de la siguiente forma:
```cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
        .WithExposedHeaders("Upload-Offset", "Location", "Upload-Length", "Tus-Version", "Tus-Resumable", "Tus-Max-Size", "Tus-Extension", "Upload-Metadata", "Upload-Defer-Length", "Upload-Concat", "Location", "Upload-Offset", "Upload-Length");
        });
});
```
Agregar singleton
```cs
services.AddSingleton(CreateTusConfiguration);
```

UseCors

```cs
app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Upload-Offset", "Location", "Upload-Length", "Tus-Version", "Tus-Resumable", "Tus-Max-Size", "Tus-Extension", "Upload-Metadata", "Upload-Defer-Length", "Upload-Concat", "Location", "Upload-Offset", "Upload-Length"); ;
});
```

DefaultConfiguration (Ver código)
```cs
DefaultTusConfiguration CreateTusConfiguration(IServiceProvider serviceProvider)
{
```
#
## *Multi-part upload*

#### Este estándar de carga de archivos permite enviar el archivo en partes separadas en un mismo body

El archivo [FileController.cs](Controllers/FileController.cs) permite manejar los métodos para carga de documentos utiliando multipart-upload.

El método del controlador que realiza la carga es POST: /file

Esto tiene una serie de validaciones, que serían interesantes implementar en la carga de archivos:
- [FileHelpers.cs](Helpers/FileHelpers.cs): Contiene métodos para la gestión de archivos.

Este objeto contiene las firmas de archivo de distintas extensiones, como una forma de validar si el archivo enviado es un virus (es extensión .pdf, pero en realidad es un .exe)
   ```cs
   private static readonly Dictionary<string, List<byte[]>> _fileSignature = new Dictionary<string, List<byte[]>>
   ```


- [MultipartRequestHelper.cs](Helpers/MultipartRequestHelper.cs): Contiene métodos para validar que dentro del objeto multipart de la solicitud, existan datos
- [PdfHelper.cs](Helpers/PdfHelper.cs): Contienen métodos de apoyo para generar la previsualización del documento.

## *Bonus!*

Esta prueba de concepto tiene un agregado, y es que al cargar un archivo usando este estándar, se cargará adicionalmente en un bucket de AWS S3.

- [IAWSHelper.cs](Interfaces/IAWSHelper.cs): Contiene las interfaces para operar las funcionalidades de carga y obtención de objetos desde S3.
- [AWSHelper.cs](Helpers/AWSHelper.cs): Contiene los métodos para operar las funcionalidades de carga y obtención de objetos desde S3.

#
## Helper
El archivo [Helper.tsx](src/helpers/Helper.tsx) tiene los métodos de consulta al API de prueba de concepto.

## *Frontend de carga de archivos*
El proyecto de backend para carga de archivos lo pueden encontrar en este link: [WebFileDemo](https://github.com/chelobone/frontend-webfiledemo)

#