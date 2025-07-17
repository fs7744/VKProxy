using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X9;

namespace VKProxy.ACME.Crypto;

public static class KeyAlgorithmProvider
{
    internal static readonly IKeyAlgorithm RS256 = new RS256Algorithm();
    internal static readonly IKeyAlgorithm ES256 = new EllipticCurveAlgorithm("P-256", "SHA-256withECDSA", "SHA256");
    internal static readonly IKeyAlgorithm ES384 = new EllipticCurveAlgorithm("P-384", "SHA-384withECDSA", "SHA384");
    internal static readonly IKeyAlgorithm ES512 = new EllipticCurveAlgorithm("P-521", "SHA-512withECDSA", "SHA512");

    public static Key NewKey(KeyAlgorithm algorithm, int? keySize = null)
    {
        var algo = Get(algorithm);
        return algo.GenerateKey(keySize);
    }

    public static IKeyAlgorithm Get(KeyAlgorithm algorithm) => algorithm switch
    {
        KeyAlgorithm.ES256 => ES256,
        KeyAlgorithm.ES384 => ES384,
        KeyAlgorithm.ES512 => ES512,
        KeyAlgorithm.RS256 => RS256,
        _ => throw new ArgumentException(nameof(algorithm))
    };

    public static Key GetKey(byte[] der)
    {
        var keyParam = PrivateKeyFactory.CreateKey(der);
        return ReadKey(keyParam);
    }

    public static Key GetKey(string pem)
    {
        using (var reader = new StringReader(pem))
        {
            var pemReader = new PemReader(reader);
            var untyped = pemReader.ReadObject();
            switch (untyped)
            {
                case AsymmetricCipherKeyPair keyPair:
                    return ReadKey(keyPair.Private);

                case AsymmetricKeyParameter keyParam:
                    return ReadKey(keyParam);

                default:
                    throw new NotSupportedException();
            }
        }
    }

    internal static (KeyAlgorithm, AsymmetricCipherKeyPair) GetKeyPair(byte[] der)
    {
        var keyParam = PrivateKeyFactory.CreateKey(der);
        return ParseKey(keyParam);
    }

    private static (KeyAlgorithm, AsymmetricCipherKeyPair) ParseKey(AsymmetricKeyParameter keyParam)
    {
        if (keyParam is RsaPrivateCrtKeyParameters)
        {
            var privateKey = (RsaPrivateCrtKeyParameters)keyParam;
            var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
            return (KeyAlgorithm.RS256, new AsymmetricCipherKeyPair(publicKey, keyParam));
        }
        else if (keyParam is ECPrivateKeyParameters privateKey)
        {
            var domain = privateKey.Parameters;
            var q = domain.G.Multiply(privateKey.D);

            DerObjectIdentifier curveId;
            KeyAlgorithm algo;
            switch (domain.Curve.FieldSize)
            {
                case 256:
                    curveId = SecObjectIdentifiers.SecP256r1;
                    algo = KeyAlgorithm.ES256;
                    break;

                case 384:
                    curveId = SecObjectIdentifiers.SecP384r1;
                    algo = KeyAlgorithm.ES384;
                    break;

                case 521:
                    curveId = SecObjectIdentifiers.SecP521r1;
                    algo = KeyAlgorithm.ES512;
                    break;

                default:
                    throw new NotSupportedException();
            }

            var publicKey = new ECPublicKeyParameters("EC", q, curveId);
            return (algo, new AsymmetricCipherKeyPair(publicKey, keyParam));
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private static Key ReadKey(AsymmetricKeyParameter keyParam)
    {
        var (algo, keyPair) = ParseKey(keyParam);
        return new AsymmetricCipherKey(algo, keyPair);
    }

    internal static ISigner GetSigner(this Key key)
    {
        var algorithm = Get(key.Algorithm);
        return algorithm.CreateSigner(key);
    }

    internal static string ToJwsAlgorithm(this KeyAlgorithm algorithm)
    {
        if (!Enum.IsDefined(typeof(KeyAlgorithm), algorithm))
        {
            throw new ArgumentException(nameof(algorithm));
        }

        return algorithm.ToString();
    }

    internal static string ToPkcsObjectId(this KeyAlgorithm algo)
    {
        switch (algo)
        {
            case KeyAlgorithm.RS256:
                return PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id;

            case KeyAlgorithm.ES256:
                return X9ObjectIdentifiers.ECDsaWithSha256.Id;

            case KeyAlgorithm.ES384:
                return X9ObjectIdentifiers.ECDsaWithSha384.Id;

            case KeyAlgorithm.ES512:
                return X9ObjectIdentifiers.ECDsaWithSha512.Id;
        }

        return null;
    }
}