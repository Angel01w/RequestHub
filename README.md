# RequestHub

## Descripcion del proyecto
RequestHub es una aplicación web diseñada para la gestión de solicitudes internas dentro de una organización, funcionando como una Mesa de Servicios donde los colaboradores pueden registrar solicitudes dirigidas a distintas áreas como TI, Mantenimiento, Transporte o Compras.

El sistema permite crear, gestionar y dar seguimiento a solicitudes mediante un flujo de estados controlado, asegurando trazabilidad de cada acción realizada. Cada solicitud incluye información como área responsable, tipo de solicitud, prioridad, descripción, adjuntos y comentarios, permitiendo a los responsables del área atenderlas de manera organizada.

El sistema implementa control de acceso basado en roles. Los Solicitantes pueden crear y consultar sus propias solicitudes, los Gestores atienden las solicitudes del área asignada y gestionan su estado, y los Administradores tienen control completo del sistema incluyendo la gestión de catálogos y cierre final de solicitudes.

Además, RequestHub incorpora historial, comentarios y filtros de búsqueda que facilitan el seguimiento y control de las solicitudes dentro de la organización.

## Tecnologias utilizadas

Backend

* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* JWT (JSON Web Token) para autenticación
* BCrypt para encriptación de contraseñas
* Swagger para documentación y pruebas de endpoints
* Arquitectura en capas

Frontend

* Vue 3 + Vite
* Vue Router
* Fetch API / Axios
* Bootstrap 5
* Diseño responsivo

## Instrucciones para correr la aplicación

Backend (.NET API)

1. Abrir el proyecto en Visual Studio.
2. Verificar que el archivo appsettings.json tenga configurada correctamente la cadena de conexión DefaultConnection.
3. Asegurarse de que la base de datos esté creada y actualizada.
4. Ejecutar la API presionando F5 o desde terminal: dotnet run
5. Verificar que Swagger esté disponible en: https://localhost:7198/swagger

Frontend (Vue)

1. Entrar a la carpeta del frontend: cd RequestFront
2. Instalar dependencias: npm install
3. Ejecutar el servidor de desarrollo: npm run dev
4. Abrir el navegador en la URL generada por Vite, normalmente: http://localhost:5173
