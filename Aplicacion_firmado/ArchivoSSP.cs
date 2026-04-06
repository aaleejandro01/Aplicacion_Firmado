using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aplicacion_firmado
{
    public class ArchivoSSP
    {
        public byte[] ContenidoOriginal { get; private set; }
        public string FirmaExtraida { get; private set; }
        public string AlgoritmoDetectado { get; private set; }
        public string HashDetectado { get; private set; }
        public bool TieneFirma { get; private set; }

        public ArchivoSSP(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo)) throw new FileNotFoundException("Archivo no encontrado", rutaArchivo);

            string[] lineas = File.ReadAllLines(rutaArchivo, Encoding.UTF8);

            // Buscamos las etiquetas desde el final hacia atrás
            int indiceSG = -1;
            int indiceSO = -1;

            for (int i = lineas.Length - 1; i >= 0; i--)
            {
                if (lineas[i].StartsWith("SG ")) indiceSG = i;
                if (lineas[i].StartsWith("SO ")) indiceSO = i;
                if (indiceSG != -1 && indiceSO != -1) break;
            }

            if (indiceSG != -1 && indiceSO != -1)
            {
                TieneFirma = true;
                // Extraer firma
                FirmaExtraida = lineas[indiceSG].Substring(3).Trim();

                // Extraer opciones (SO Algoritmo:Hash)
                string opciones = lineas[indiceSO].Substring(3).Trim();
                string[] partes = opciones.Split(':');
                if (partes.Length == 2)
                {
                    AlgoritmoDetectado = partes[0];
                    HashDetectado = partes[1];
                }

                // Reconstruir el contenido original (todo lo que está ANTES de SO)
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < indiceSO; i++)
                {
                    sb.AppendLine(lineas[i]);
                }
                // Quitamos el último salto de línea extra que añade AppendLine
                ContenidoOriginal = Encoding.UTF8.GetBytes(sb.ToString().TrimEnd('\r', '\n'));
            }
            else
            {
                TieneFirma = false;
                ContenidoOriginal = File.ReadAllBytes(rutaArchivo);
            }
        }

        public static void IncrustarFirma(string rutaArchivo, string firmaBase64, string algoritmo, string hash)
        {
            // Añadimos las dos líneas al final del archivo
            string bloqueFirma = $"{Environment.NewLine}SO {algoritmo}:{hash}{Environment.NewLine}SG {firmaBase64}";
            File.AppendAllText(rutaArchivo, bloqueFirma, Encoding.UTF8);
        }
    }
}
