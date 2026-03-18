using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Aplicacion_firmado
{
    public class FirmwareSigner : IDisposable
    {
        private AsymmetricKeyParameter _clavePrivada;
        private string _algoritmoFirma;
        private string _algoritmoHash;

        public FirmwareSigner(string clavePrivadaPem, string algoritmoFirma, string algoritmoHash)
        {
            _algoritmoFirma = algoritmoFirma;
            _algoritmoHash = algoritmoHash;

            // Leemos el archivo .pem y extraemos la fórmula matemática de la clave
            using (var reader = new StringReader(clavePrivadaPem))
            {
                var pemReader = new PemReader(reader);
                var pemObject = pemReader.ReadObject();

                if (pemObject is AsymmetricCipherKeyPair keyPair)
                    _clavePrivada = keyPair.Private;
                else if (pemObject is AsymmetricKeyParameter keyParam && keyParam.IsPrivate)
                    _clavePrivada = keyParam;
                else
                    throw new ArgumentException("El archivo no contiene una clave privada válida.");
            }
        }

        public byte[] FirmarDatos(byte[] datosOriginales)
        {
            //Algoritmo EdDSA (Ed25519)

            if (_algoritmoFirma == "EdDSA Ed25519")
            {
                // Comprobación de seguridad matemática
                if (!(_clavePrivada is Ed25519PrivateKeyParameters))
                    throw new InvalidOperationException("La clave leída no es del tipo Ed25519.");

                // Ed25519 no usa un hash previo de forma tradicional, firma el mensaje crudo
                var edSigner = new Ed25519Signer();
                edSigner.Init(true, _clavePrivada);
                edSigner.BlockUpdate(datosOriginales, 0, datosOriginales.Length);
                return edSigner.GenerateSignature();
            }

            //Familia ECDSA (192, 256, 384)

            if (_algoritmoFirma.StartsWith("ECDSA"))
            {
                // Comprobación de seguridad matemática
                if (!(_clavePrivada is ECPrivateKeyParameters))
                    throw new InvalidOperationException($"La clave leída no sirve para {_algoritmoFirma}.");

                // 1. Elegimos el Hash según lo que pidió el usuario
                IDigest digest = _algoritmoHash switch
                {
                    "SHA-256" => new Sha256Digest(),
                    "SHA-512" => new Sha512Digest(),
                    "BLAKE2" => new Blake2bDigest(256), // Versión de 256 bits
                    "BLAKE3" => new Blake3Digest(256),  // Versión de 256 bits
                    _ => throw new NotSupportedException($"El hash {_algoritmoHash} no está soportado.")
                };

                // 2. Hasheamos el archivo original manualmente
                byte[] hashCalculado = new byte[digest.GetDigestSize()];
                digest.BlockUpdate(datosOriginales, 0, datosOriginales.Length);
                digest.DoFinal(hashCalculado, 0);

                // 3. Firmamos matemáticamente ese Hash ("NONEwithECDSA" porque el hash ya está hecho)
                ISigner ecdsaSigner = SignerUtilities.GetSigner("NONEwithECDSA");
                ecdsaSigner.Init(true, _clavePrivada);
                ecdsaSigner.BlockUpdate(hashCalculado, 0, hashCalculado.Length);
                return ecdsaSigner.GenerateSignature();
            }

            throw new NotSupportedException($"El algoritmo {_algoritmoFirma} es desconocido.");
        }

        public void Dispose()
        {
            // Se usa para limpiar memoria si fuera necesario (obligatorio por el IDisposable)
        }
    }
}

