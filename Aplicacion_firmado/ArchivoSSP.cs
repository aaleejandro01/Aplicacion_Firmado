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
        public bool TieneFirma { get; private set; }


        //Constructor de la clase
        public ArchivoSSP(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                throw new FileNotFoundException("El archivo no existe.", rutaArchivo);
            }

            string todoElTexto = File.ReadAllText(rutaArchivo, Encoding.UTF8);

            // Buscamos si la última parte del archivo contiene nuestro marcador "SG "
            int indiceFirma = todoElTexto.LastIndexOf("SG ");

            // Verificamos si encontró el marcador y si no está en la primera línea
            if (indiceFirma != -1 && indiceFirma > 0)
            {
                // 1. Extraemos la firma pura (saltándonos el "SG ")
                FirmaExtraida = todoElTexto.Substring(indiceFirma + 3).Trim();
                TieneFirma = true;

                // 2. Calculamos dónde cortar para quitar el salto de línea previo al "SG "
                int puntoCorte = indiceFirma;

                // Comprobamos si el salto de línea es de Windows (\r\n) o de Linux (\n)
                if (indiceFirma >= 2 && todoElTexto.Substring(indiceFirma - 2, 2) == "\r\n")
                    puntoCorte -= 2;
                else if (indiceFirma >= 1 && todoElTexto[indiceFirma - 1] == '\n')
                    puntoCorte -= 1;

                // 3. Extraemos el texto original intacto y lo pasamos a bytes
                string textoOriginal = todoElTexto.Substring(0, puntoCorte);
                ContenidoOriginal = Encoding.UTF8.GetBytes(textoOriginal);
            }
            else
            {
                // Si no tiene firma, el contenido original es todo el archivo
                TieneFirma = false;
                ContenidoOriginal = Encoding.UTF8.GetBytes(todoElTexto);
                FirmaExtraida = string.Empty;
            }
        }

        // Método estático para añadir la firma al final del archivo físico
        public static void IncrustarFirma(string rutaArchivo, string firmaBase64)
        {
            // Environment.NewLine asegura que ponga el salto de línea correcto según el sistema operativo
            string lineaFirma = Environment.NewLine + "SG " + firmaBase64;
            File.AppendAllText(rutaArchivo, lineaFirma, Encoding.UTF8);
        }
    }
}
