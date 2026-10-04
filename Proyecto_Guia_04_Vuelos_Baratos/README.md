# Guía de Prácticas #04 - Encuentro de vuelos baratos

Proyecto académico en C# que modela una red de vuelos como un grafo dirigido y ponderado.

## Requisitos
- .NET 8 SDK o Visual Studio 2022 con soporte para .NET 8.

## Ejecución
1. Coloque `Program.cs`, `vuelos.txt` y `GuiaPracticas04Vuelos.csproj` en la misma carpeta.
2. Abra una terminal en esa carpeta.
3. Ejecute:
   ```bash
   dotnet run
   ```
4. Use la opción 4 para buscar la ruta más barata.

## Prueba recomendada para la evidencia
- Origen: `UIO`
- Destino: `GPS`
- Resultado esperado por costo: `UIO -> CUE -> GYE -> GPS`
- Costo total esperado: `USD 200.00`
- El tiempo exacto se mide en cada ejecución con `Stopwatch` y puede variar según el equipo.

## Estructura
- `Program.cs`: implementación del grafo, Dijkstra, reportería y menú.
- `vuelos.txt`: base de datos ficticia.
- `GuiaPracticas04Vuelos.csproj`: archivo de proyecto.

## IA utilizada
La versión base del código fue generada con apoyo de ChatGPT (OpenAI). Si se usa sin modificaciones, el porcentaje aproximado de código generado con IA es 100 %. Si se realizan cambios propios, actualizar este porcentaje en el informe.
