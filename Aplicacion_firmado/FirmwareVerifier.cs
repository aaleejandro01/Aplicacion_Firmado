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
    public class FirmwareVerifier : IDisposable
    {
        private AsymmetricKeyParameter _clavePublica; // SOLO necesitamos la pública
        private string _algoritmoFirma;
        private string _algoritmoHash;

        public FirmwareVerifier(string clavePem, string algoritmoFirma, string algoritmoHash)
        {
            _algoritmoFirma = algoritmoFirma;
            _algoritmoHash = algoritmoHash;

            using (var reader = new StringReader(clavePem))
            {
                var pemReader = new PemReader(reader);
                var pemObject = pemReader.ReadObject();

                // Si el archivo tiene el par completo (como los que generamos), sacamos solo la pública
                if (pemObject is AsymmetricCipherKeyPair keyPair)
                {
                    _clavePublica = keyPair.Public;
                }
                // Si el archivo es estrictamente una clave pública pura, la cogemos directamente
                else if (pemObject is AsymmetricKeyParameter keyParam && !keyParam.IsPrivate)
                {
                    _clavePublica = keyParam;
                }
                else
                {
                    throw new ArgumentException("El archivo proporcionado no contiene una clave pública válida para verificar.");
                }
            }
        }

        public bool VerificarFirma(byte[] datosOriginales, byte[] firmaCandidata)
        {

            // Algoritmo EdDSA (Ed25519)

            if (_algoritmoFirma == "EdDSA Ed25519")
            {
                var edVerifier = new Ed25519Signer();
                // El parámetro 'false' le indica a BouncyCastle que estamos VERIFICANDO
                edVerifier.Init(false, _clavePublica);
                edVerifier.BlockUpdate(datosOriginales, 0, datosOriginales.Length);
                return edVerifier.VerifySignature(firmaCandidata);
            }


            // Familia ECDSA (192, 256, 384)

            if (_algoritmoFirma.StartsWith("ECDSA"))
            {
                IDigest digest = _algoritmoHash switch
                {
                    "SHA-256" => new Sha256Digest(),
                    "SHA-512" => new Sha512Digest(),
                    "BLAKE2" => new Blake2bDigest(256),
                    "BLAKE3" => new Blake3Digest(256),
                    _ => throw new NotSupportedException($"El hash {_algoritmoHash} no está soportado.")
                };

                // Hasheamos el archivo original para compararlo
                byte[] hashCalculado = new byte[digest.GetDigestSize()];
                digest.BlockUpdate(datosOriginales, 0, datosOriginales.Length);
                digest.DoFinal(hashCalculado, 0);

                ISigner ecdsaVerifier = SignerUtilities.GetSigner("NONEwithECDSA");
                // El parámetro 'false' indica modo VERIFICACIÓN con clave PÚBLICA
                ecdsaVerifier.Init(false, _clavePublica);
                ecdsaVerifier.BlockUpdate(hashCalculado, 0, hashCalculado.Length);
                return ecdsaVerifier.VerifySignature(firmaCandidata);
            }

            return false;
        }

        public void Dispose()
        {
            // Limpieza de memoria requerida por IDisposable
        }
    }
}
