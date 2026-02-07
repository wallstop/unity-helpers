# Unity Package Signing

This document explains how the Unity Helpers package is signed for Unity 6.3+ compatibility.

## Overview

Unity 6.3 introduced a package signature verification system that shows warnings for packages without signatures. This repository implements automatic package signing to suppress these warnings while maintaining full backwards compatibility.

## How It Works

### Unsigned Mode (Default & Recommended)

By default, the package is marked as `"unsigned"` in `package.json`. This:

- ✅ Suppresses Unity 6.3+ warnings about missing signatures
- ✅ Is backwards compatible with Unity 2021.3+
- ✅ Works with all distribution methods (OpenUPM, NPM, Git URLs, manual export)
- ✅ Requires no additional infrastructure or keys

**This is the recommended approach for community packages.**

### Cryptographic Signing (Optional)

For organizations that want to implement cryptographic signatures:

- Generate an RSA key pair
- Store the private key as a GitHub secret
- Configure CI/CD to sign on release
- Unity will see the signature but may not verify third-party keys

## Manual Signing

### Sign as Unsigned

```bash
# Mark package as unsigned (recommended)
npm run sign:package

# Dry run to preview changes
npm run sign:package:dry-run
```

### Sign with Private Key

```bash
# Using command line arguments
node scripts/sign-package.js --private-key ./private-key.pem --key-id my-key-2024

# Using environment variables (for CI/CD)
export UNITY_PACKAGE_PRIVATE_KEY="$(base64 < private-key.pem)"
export UNITY_PACKAGE_KEY_ID="ci-signing-key"
npm run sign:package
```

## CI/CD Integration

The package is automatically signed when releases are published via the `sign-package.yml` workflow:

1. **On Release Published**: Automatically signs the package as "unsigned"
2. **Manual Dispatch**: Can optionally sign with a private key if secrets are configured

### Setting Up Cryptographic Signing (Optional)

If you want to use cryptographic signatures:

1. **Generate RSA Key Pair**:

   ```bash
   # Generate 2048-bit RSA private key
   openssl genrsa -out private-key.pem 2048

   # Extract public key
   openssl rsa -in private-key.pem -pubout -out public-key.pem
   ```

2. **Configure GitHub Secrets**:

   - `UNITY_PACKAGE_PRIVATE_KEY`: Base64 encoded private key

     ```bash
     base64 < private-key.pem
     ```

   - `UNITY_PACKAGE_KEY_ID`: Identifier for the key (e.g., "unity-helpers-2024")

3. **Trigger Signing Workflow**:
   - Go to Actions → Sign Package → Run workflow
   - Select "Sign with private key: true"

## Distribution Compatibility

The signing solution works with all distribution methods:

| Distribution Method | Compatible | Notes                                      |
| ------------------- | ---------- | ------------------------------------------ |
| OpenUPM             | ✅ Yes     | Packages sync from NPM or manual upload    |
| NPM Registry        | ✅ Yes     | Signature field is preserved               |
| Git URLs            | ✅ Yes     | Users get signed package.json              |
| Manual Export       | ✅ Yes     | .unitypackage includes signed package.json |

## Unity Version Compatibility

| Unity Version      | Behavior                                       |
| ------------------ | ---------------------------------------------- |
| Unity 2021.3 - 6.2 | Ignores signature field (backwards compatible) |
| Unity 6.3+         | Reads signature field, suppresses warning      |

## Signature Format

The signature field in `package.json` can be:

### String Format (Unsigned)

```json
{
  "signature": "unsigned"
}
```

### Object Format (Cryptographic)

```json
{
  "signature": {
    "signature": "base64-encoded-signature",
    "keyId": "key-identifier",
    "algorithm": "RS256",
    "signedAt": "2024-02-07T21:00:00.000Z",
    "packageHash": "sha256-hash-of-package-contents"
  }
}
```

## Script Reference

### `scripts/sign-package.js`

Signing script with comprehensive options:

**Usage:**

```bash
node scripts/sign-package.js [options]
```

**Options:**

- `--private-key <path>`: Path to RSA private key (PEM format)
- `--key-id <id>`: Key identifier
- `--dry-run`: Preview changes without modifying files
- `--help`: Show help message

**Environment Variables:**

- `UNITY_PACKAGE_PRIVATE_KEY`: Base64 encoded private key (for CI/CD)
- `UNITY_PACKAGE_KEY_ID`: Key identifier

**Examples:**

```bash
# Mark as unsigned (default, recommended)
npm run sign:package

# Preview changes
npm run sign:package:dry-run

# Sign with private key (local development)
node scripts/sign-package.js --private-key ./key.pem --key-id dev-key

# Sign with environment variables (CI/CD)
export UNITY_PACKAGE_PRIVATE_KEY="$(base64 < private-key.pem)"
export UNITY_PACKAGE_KEY_ID="ci-key-2024"
npm run sign:package
```

## Troubleshooting

### "Package is unsigned" warning still appears

1. Verify `package.json` has the `signature` field:

   ```bash
   jq '.signature' package.json
   ```

   Should return `"unsigned"` or a signature object.

2. Clear Unity's package cache:
   - Close Unity
   - Delete `Library/PackageCache`
   - Reopen Unity and reimport package

3. Verify Unity version is 6.3 or higher:
   - Unity 6.2 and below don't have this warning

### Cryptographic signature not working

1. Verify you're using RSA keys in PEM format:

   ```bash
   openssl rsa -in private-key.pem -check
   ```

2. Ensure both `UNITY_PACKAGE_PRIVATE_KEY` and `UNITY_PACKAGE_KEY_ID` are set

3. Check the workflow logs for detailed error messages

### Package.json gets overwritten

The signing workflow commits changes to `package.json`. If you're working on a branch:

- Pull the latest changes after the workflow runs
- Use `--dry-run` locally to preview changes

## References

- [Unity Package Signature Documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-signature.html)
- [Unity Package Export Documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-export.html)
- [OpenSSL Documentation](https://www.openssl.org/docs/)

## License

This signing implementation is part of Unity Helpers and is licensed under the MIT License.
