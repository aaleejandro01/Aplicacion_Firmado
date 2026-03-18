using System;
using System.IO;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Aplicacion_firmado
{
    public class GeneradorClaves
    {
        // Esta función recibe el nombre del algoritmo y dónde quieres guardar el archivo .pem
        public static void CrearClavePrivada(string algoritmo, string rutaArchivoPem)
        {
            AsymmetricCipherKeyPair parDeClaves = null;
            SecureRandom generadorAleatorio = new SecureRandom(); // El motor de entropía segura

            // 1. Si es de la familia ECDSA, buscamos la curva matemática correcta
            if (algoritmo.StartsWith("ECDSA"))
            {
                string nombreCurva = algoritmo switch
                {
                    "ECDSA-192" => "secp192r1",
                    "ECDSA-256" => "secp256r1",
                    "ECDSA-384" => "secp384r1",
                    _ => throw new Exception("Curva ECDSA no soportada")
                };

                var parametrosCurva = SecNamedCurves.GetByName(nombreCurva);
                var parametrosDominio = new ECDomainParameters(parametrosCurva.Curve, parametrosCurva.G, parametrosCurva.N, parametrosCurva.H, parametrosCurva.GetSeed());
                var parametrosGeneracion = new ECKeyGenerationParameters(parametrosDominio, generadorAleatorio);

                var generadorEcdsa = new ECKeyPairGenerator();
                generadorEcdsa.Init(parametrosGeneracion);
                parDeClaves = generadorEcdsa.GenerateKeyPair();
            }
            // 2. Si es Ed25519, usamos su propio generador
            else if (algoritmo == "EdDSA Ed25519")
            {
                var generadorEd = new Ed25519KeyPairGenerator();
                generadorEd.Init(new Ed25519KeyGenerationParameters(generadorAleatorio));
                parDeClaves = generadorEd.GenerateKeyPair();
            }

            // 3. Escribimos la clave generada en un archivo de texto con el formato estándar PEM
            using (var escritorTexto = new StreamWriter(rutaArchivoPem))
            {
                var escritorPem = new PemWriter(escritorTexto);
                escritorPem.WriteObject(parDeClaves.Private);
            }
        }
    }
}
