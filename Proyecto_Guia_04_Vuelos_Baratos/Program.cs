using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GuiaPracticas04Vuelos
{
    public class Vuelo
    {
        public string Origen { get; }
        public string Destino { get; }
        public double Precio { get; }

        public Vuelo(string origen, string destino, double precio)
        {
            Origen = origen;
            Destino = destino;
            Precio = precio;
        }
    }

    public class GrafoVuelos
    {
        private readonly Dictionary<string, List<Vuelo>> adyacencia =
            new(StringComparer.OrdinalIgnoreCase);

        public int CantidadAeropuertos => adyacencia.Count;
        public int CantidadVuelos => adyacencia.Values.Sum(lista => lista.Count);

        public IEnumerable<string> Aeropuertos =>
            adyacencia.Keys.OrderBy(codigo => codigo);

        public void AgregarVuelo(string origen, string destino, double precio)
        {
            if (precio < 0)
                throw new ArgumentException("El precio no puede ser negativo.");

            origen = origen.Trim().ToUpperInvariant();
            destino = destino.Trim().ToUpperInvariant();

            if (!adyacencia.ContainsKey(origen))
                adyacencia[origen] = new List<Vuelo>();

            if (!adyacencia.ContainsKey(destino))
                adyacencia[destino] = new List<Vuelo>();

            adyacencia[origen].Add(new Vuelo(origen, destino, precio));
        }

        public bool ContieneAeropuerto(string codigo) =>
            adyacencia.ContainsKey(codigo);

        public void CargarDesdeArchivo(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
                throw new FileNotFoundException("No se encontró el archivo de vuelos.", rutaArchivo);

            int numeroLinea = 0;

            foreach (string lineaOriginal in File.ReadLines(rutaArchivo))
            {
                numeroLinea++;
                string linea = lineaOriginal.Trim();

                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("#"))
                    continue;

                string[] partes = linea.Split(';');
                if (partes.Length != 3)
                    throw new FormatException($"Formato inválido en la línea {numeroLinea}: {lineaOriginal}");

                if (!double.TryParse(
                        partes[2],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double precio))
                {
                    throw new FormatException($"Precio inválido en la línea {numeroLinea}: {partes[2]}");
                }

                AgregarVuelo(partes[0], partes[1], precio);
            }
        }

        public IReadOnlyList<Vuelo> ObtenerVuelosDesde(string origen)
        {
            if (!adyacencia.TryGetValue(origen, out List<Vuelo>? vuelos))
                return Array.Empty<Vuelo>();

            return vuelos.OrderBy(v => v.Destino).ThenBy(v => v.Precio).ToList();
        }

        public IEnumerable<Vuelo> ObtenerTodosLosVuelos()
        {
            return adyacencia.Values
                .SelectMany(lista => lista)
                .OrderBy(v => v.Origen)
                .ThenBy(v => v.Destino)
                .ThenBy(v => v.Precio);
        }

        public (List<string> Ruta, double Costo, long Ticks, double Microsegundos)
            EncontrarRutaMasBarata(string origen, string destino)
        {
            origen = origen.Trim().ToUpperInvariant();
            destino = destino.Trim().ToUpperInvariant();

            if (!ContieneAeropuerto(origen) || !ContieneAeropuerto(destino))
                return (new List<string>(), double.PositiveInfinity, 0, 0);

            var cronometro = Stopwatch.StartNew();

            var distancias = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var anterior = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var cola = new PriorityQueue<string, double>();

            foreach (string aeropuerto in adyacencia.Keys)
            {
                distancias[aeropuerto] = double.PositiveInfinity;
                anterior[aeropuerto] = null;
            }

            distancias[origen] = 0;
            cola.Enqueue(origen, 0);

            while (cola.TryDequeue(out string? actual, out double distanciaActual))
            {
                if (actual is null)
                    continue;

                if (distanciaActual > distancias[actual])
                    continue;

                if (actual.Equals(destino, StringComparison.OrdinalIgnoreCase))
                    break;

                foreach (Vuelo vuelo in adyacencia[actual])
                {
                    double nuevaDistancia = distancias[actual] + vuelo.Precio;

                    if (nuevaDistancia < distancias[vuelo.Destino])
                    {
                        distancias[vuelo.Destino] = nuevaDistancia;
                        anterior[vuelo.Destino] = actual;
                        cola.Enqueue(vuelo.Destino, nuevaDistancia);
                    }
                }
            }

            cronometro.Stop();
            double microsegundos = cronometro.ElapsedTicks * 1_000_000.0 / Stopwatch.Frequency;

            if (double.IsPositiveInfinity(distancias[destino]))
                return (new List<string>(), double.PositiveInfinity, cronometro.ElapsedTicks, microsegundos);

            var ruta = new List<string>();
            string? cursor = destino;

            while (cursor is not null)
            {
                ruta.Add(cursor);
                cursor = anterior[cursor];
            }

            ruta.Reverse();
            return (ruta, distancias[destino], cronometro.ElapsedTicks, microsegundos);
        }
    }

    internal class Program
    {
        private static readonly Dictionary<string, string> NombresAeropuertos =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["UIO"] = "Quito",
                ["GYE"] = "Guayaquil",
                ["CUE"] = "Cuenca",
                ["MEC"] = "Manta",
                ["LOH"] = "Loja",
                ["GPS"] = "Baltra - Galápagos"
            };

        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var grafo = new GrafoVuelos();

            string rutaArchivo = Path.Combine(AppContext.BaseDirectory, "vuelos.txt");

            try
            {
                grafo.CargarDesdeArchivo(rutaArchivo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la base de datos: {ex.Message}");
                return;
            }

            bool continuar = true;
            while (continuar)
            {
                MostrarEncabezado(grafo);
                Console.WriteLine("1. Listar aeropuertos");
                Console.WriteLine("2. Listar todos los vuelos");
                Console.WriteLine("3. Consultar vuelos desde un aeropuerto");
                Console.WriteLine("4. Buscar la ruta más barata");
                Console.WriteLine("5. Mostrar lista de adyacencia del grafo");
                Console.WriteLine("6. Salir");
                Console.Write("Seleccione una opción: ");

                string? opcion = Console.ReadLine();
                Console.WriteLine();

                switch (opcion)
                {
                    case "1":
                        ListarAeropuertos(grafo);
                        break;
                    case "2":
                        ListarVuelos(grafo);
                        break;
                    case "3":
                        ConsultarVuelosDesde(grafo);
                        break;
                    case "4":
                        BuscarRutaMasBarata(grafo);
                        break;
                    case "5":
                        MostrarAdyacencia(grafo);
                        break;
                    case "6":
                        continuar = false;
                        Console.WriteLine("Programa finalizado.");
                        break;
                    default:
                        Console.WriteLine("Opción no válida.");
                        break;
                }

                if (continuar)
                {
                    Console.WriteLine("\nPresione ENTER para continuar...");
                    Console.ReadLine();
                    Console.Clear();
                }
            }
        }

        private static void MostrarEncabezado(GrafoVuelos grafo)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("  SISTEMA DE BÚSQUEDA DE VUELOS BARATOS - GRAFO PONDERADO");
            Console.WriteLine("============================================================");
            Console.WriteLine($"Aeropuertos: {grafo.CantidadAeropuertos} | Vuelos: {grafo.CantidadVuelos}\n");
        }

        private static string Nombre(string codigo) =>
            NombresAeropuertos.TryGetValue(codigo, out string? nombre)
                ? $"{codigo} - {nombre}"
                : codigo;

        private static void ListarAeropuertos(GrafoVuelos grafo)
        {
            Console.WriteLine("AEROPUERTOS REGISTRADOS");
            Console.WriteLine("------------------------");
            foreach (string codigo in grafo.Aeropuertos)
                Console.WriteLine($"• {Nombre(codigo)}");
        }

        private static void ListarVuelos(GrafoVuelos grafo)
        {
            Console.WriteLine("REPORTE GENERAL DE VUELOS");
            Console.WriteLine("-------------------------");
            foreach (Vuelo vuelo in grafo.ObtenerTodosLosVuelos())
            {
                Console.WriteLine(
                    $"{Nombre(vuelo.Origen),-26} -> {Nombre(vuelo.Destino),-26} | USD {vuelo.Precio,7:F2}");
            }
        }

        private static void ConsultarVuelosDesde(GrafoVuelos grafo)
        {
            Console.Write("Código del aeropuerto de origen: ");
            string origen = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (!grafo.ContieneAeropuerto(origen))
            {
                Console.WriteLine("Aeropuerto no encontrado.");
                return;
            }

            IReadOnlyList<Vuelo> vuelos = grafo.ObtenerVuelosDesde(origen);
            Console.WriteLine($"\nVuelos desde {Nombre(origen)}:");

            if (vuelos.Count == 0)
            {
                Console.WriteLine("No existen vuelos de salida registrados.");
                return;
            }

            foreach (Vuelo vuelo in vuelos)
                Console.WriteLine($"-> {Nombre(vuelo.Destino)} | USD {vuelo.Precio:F2}");
        }

        private static void BuscarRutaMasBarata(GrafoVuelos grafo)
        {
            Console.Write("Aeropuerto de origen: ");
            string origen = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            Console.Write("Aeropuerto de destino: ");
            string destino = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            var resultado = grafo.EncontrarRutaMasBarata(origen, destino);

            if (resultado.Ruta.Count == 0)
            {
                Console.WriteLine("No existe una ruta disponible entre los aeropuertos indicados.");
                return;
            }

            Console.WriteLine("\nRESULTADO DE LA BÚSQUEDA");
            Console.WriteLine("-------------------------");
            Console.WriteLine("Ruta: " + string.Join(" -> ", resultado.Ruta));
            Console.WriteLine($"Costo total: USD {resultado.Costo:F2}");
            Console.WriteLine($"Tiempo del algoritmo: {resultado.Microsegundos:F3} µs");
            Console.WriteLine($"Ticks medidos por Stopwatch: {resultado.Ticks}");
        }

        private static void MostrarAdyacencia(GrafoVuelos grafo)
        {
            Console.WriteLine("LISTA DE ADYACENCIA");
            Console.WriteLine("-------------------");

            foreach (string aeropuerto in grafo.Aeropuertos)
            {
                IReadOnlyList<Vuelo> vuelos = grafo.ObtenerVuelosDesde(aeropuerto);
                string conexiones = vuelos.Count == 0
                    ? "sin conexiones de salida"
                    : string.Join(", ", vuelos.Select(v => $"{v.Destino}(USD {v.Precio:F2})"));

                Console.WriteLine($"{aeropuerto} -> {conexiones}");
            }
        }
    }
}
