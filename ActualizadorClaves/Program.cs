using System;
using System.IO;

namespace ActualizadorClaves
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Actualizador de Seguridad - Cliente";
            Console.WriteLine("==============================================");
            Console.WriteLine("     INSTALADOR DE CLAVES DE VERIFICACIÓN     ");
            Console.WriteLine("==============================================\n");

            try
            {
                string directorioActual = AppDomain.CurrentDomain.BaseDirectory;
                string rutaNuevaClave = Path.Combine(directorioActual, "nueva_clave.pem");
                string rutaInfo = Path.Combine(directorioActual, "info.txt");

                if (!File.Exists(rutaNuevaClave) || !File.Exists(rutaInfo))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] Paquete corrupto. Extrae todo el .zip antes de ejecutar.");
                    Console.ResetColor();
                    Console.ReadLine();
                    return;
                }

                string algoritmo = File.ReadAllText(rutaInfo).Trim();
                string nombreArchivoDestino = $"{algoritmo}_publica.pem";

                Console.WriteLine("Buscando instalación de la aplicación cliente...");

                // INTENTO 1: Busca la carpeta "Claves" en el mismo directorio donde se ejecuta
                string carpetaDestino = Path.Combine(directorioActual, "Claves");

                // Si no está ahí, le pide al cliente que le diga dónde la instaló
                if (!Directory.Exists(carpetaDestino))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[AVISO] No se encontró la carpeta 'Claves' automáticamente.");
                    Console.ResetColor();
                    Console.WriteLine("Por favor, introduce la ruta donde está instalada la aplicación:");
                    Console.Write("> ");
                    string rutaManual = Console.ReadLine().Trim().Trim('"');
                    carpetaDestino = Path.Combine(rutaManual, "Claves");

                    if (!Directory.Exists(carpetaDestino))
                    {
                        // Si la carpeta Claves no existe en la app del cliente, la crea automáticamente
                        Directory.CreateDirectory(carpetaDestino);
                        Console.WriteLine("Carpeta 'Claves' creada en el directorio destino.");
                    }
                }

                string rutaDestinoCompleta = Path.Combine(carpetaDestino, nombreArchivoDestino);

                // BACKUP para el cliente
                if (File.Exists(rutaDestinoCompleta))
                {
                    string fechaBackup = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string rutaBackup = Path.Combine(carpetaDestino, $"{algoritmo}_backup_{fechaBackup}_publica.pem");
                    File.Move(rutaDestinoCompleta, rutaBackup);
                }

                // INSTALACIÓN
                File.Copy(rutaNuevaClave, rutaDestinoCompleta);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ ACTUALIZACIÓN COMPLETADA.");
                Console.WriteLine($"La nueva clave para {algoritmo} se ha instalado correctamente en la App Cliente.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR CRÍTICO] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}