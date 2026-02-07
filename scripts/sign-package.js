#!/usr/bin/env node

/**
 * Unity Package Signing Script
 *
 * This script generates a cryptographic signature for Unity packages compatible with Unity 6.3+.
 * It calculates a SHA256 hash of the package contents and signs it with a private key.
 *
 * Usage:
 *   node scripts/sign-package.js [options]
 *
 * Options:
 *   --private-key <path>  Path to private key file (default: use unsigned)
 *   --key-id <id>         Key identifier for the signature
 *   --dry-run             Show what would be done without modifying files
 *   --help                Show this help message
 *
 * Environment Variables:
 *   UNITY_PACKAGE_PRIVATE_KEY  Base64 encoded private key (for CI/CD)
 *   UNITY_PACKAGE_KEY_ID       Key identifier
 *
 * If no private key is provided, the package will be marked as "unsigned".
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

// Configuration
const PACKAGE_JSON_PATH = path.join(__dirname, '..', 'package.json');
const SIGNATURE_ALGORITHM = 'RS256';

// Files to include in signature calculation (relative to package root)
const SIGNATURE_INCLUDES = [
  'package.json',
  'Editor/**/*.cs',
  'Runtime/**/*.cs',
  'Shaders/**/*.shader',
  'Shaders/**/*.cginc',
  'URP/**/*.cs',
  'Tests/**/*.cs',
];

// Files to exclude from signature calculation
const SIGNATURE_EXCLUDES = [
  '**/*.meta',
  '**/node_modules/**',
  '**/.*',
  'Samples~/**',
];

/**
 * Parse command line arguments
 */
function parseArgs() {
  const args = process.argv.slice(2);
  const options = {
    privateKeyPath: null,
    keyId: process.env.UNITY_PACKAGE_KEY_ID || null,
    dryRun: false,
    help: false,
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    switch (arg) {
      case '--private-key':
        options.privateKeyPath = args[++i];
        break;
      case '--key-id':
        options.keyId = args[++i];
        break;
      case '--dry-run':
        options.dryRun = true;
        break;
      case '--help':
      case '-h':
        options.help = true;
        break;
      default:
        console.error(`Unknown option: ${arg}`);
        process.exit(1);
    }
  }

  return options;
}

/**
 * Show help message
 */
function showHelp() {
  const helpText = `
Unity Package Signing Script

This script generates cryptographic signatures for Unity packages compatible with Unity 6.3+.

Usage:
  node scripts/sign-package.js [options]

Options:
  --private-key <path>  Path to private key file (PEM format)
  --key-id <id>         Key identifier for the signature
  --dry-run             Show what would be done without modifying files
  --help, -h            Show this help message

Environment Variables:
  UNITY_PACKAGE_PRIVATE_KEY  Base64 encoded private key (for CI/CD)
  UNITY_PACKAGE_KEY_ID       Key identifier for signing

Signing Methods:
  1. With Private Key: Generates cryptographic signature (requires RSA key pair)
     - Provide key via --private-key flag or UNITY_PACKAGE_PRIVATE_KEY env var
     - Must also provide --key-id or UNITY_PACKAGE_KEY_ID env var
  
  2. Without Key (Default): Marks package as "unsigned"
     - Explicitly tells Unity 6.3+ the package is intentionally unsigned
     - Suppresses Unity's "missing signature" warning
     - Recommended for community packages without signing infrastructure

Distribution Compatibility:
  - OpenUPM: ✓ Compatible (works with both signed and unsigned)
  - NPM: ✓ Compatible (works with both signed and unsigned)
  - Git URLs: ✓ Compatible (works with both signed and unsigned)
  - Manual Export: ✓ Compatible (works with both signed and unsigned)

Examples:
  # Mark as unsigned (suppresses Unity warning)
  node scripts/sign-package.js

  # Sign with private key
  node scripts/sign-package.js --private-key ./private-key.pem --key-id my-key-2024

  # Sign using environment variable (CI/CD)
  export UNITY_PACKAGE_PRIVATE_KEY="$(base64 < private-key.pem)"
  export UNITY_PACKAGE_KEY_ID="ci-signing-key"
  node scripts/sign-package.js

  # Dry run to see what would be done
  node scripts/sign-package.js --dry-run
`;
  console.log(helpText);
}

/**
 * Calculate SHA256 hash of package contents
 * For now, we'll hash the package.json content minus the signature field
 * @returns {string} Hex encoded SHA256 hash
 */
function calculatePackageHash(packageJson) {
  // Create a copy without the signature field for hash calculation
  const packageForHashing = { ...packageJson };
  delete packageForHashing.signature;

  // Sort keys for consistent hashing
  const sortedPackage = JSON.stringify(packageForHashing, Object.keys(packageForHashing).sort(), 2);
  
  const hash = crypto.createHash('sha256');
  hash.update(sortedPackage);
  return hash.digest('hex');
}

/**
 * Sign package with private key
 * @param {string} privateKeyPath Path to private key or base64 encoded key
 * @param {string} keyId Key identifier
 * @param {object} packageJson Package.json object
 * @returns {object} Signature object
 */
function signPackage(privateKeyPath, keyId, packageJson) {
  let privateKey;

  // Try to load private key from environment variable first (for CI/CD)
  if (process.env.UNITY_PACKAGE_PRIVATE_KEY) {
    console.log('Using private key from UNITY_PACKAGE_PRIVATE_KEY environment variable');
    try {
      privateKey = Buffer.from(process.env.UNITY_PACKAGE_PRIVATE_KEY, 'base64').toString('utf-8');
    } catch (err) {
      console.error('Failed to decode private key from environment variable:', err.message);
      process.exit(1);
    }
  } else if (privateKeyPath) {
    if (!fs.existsSync(privateKeyPath)) {
      console.error(`Private key file not found: ${privateKeyPath}`);
      process.exit(1);
    }
    privateKey = fs.readFileSync(privateKeyPath, 'utf-8');
  } else {
    console.error('No private key provided');
    process.exit(1);
  }

  if (!keyId) {
    console.error('Key ID is required for signing. Provide --key-id or set UNITY_PACKAGE_KEY_ID');
    process.exit(1);
  }

  // Calculate hash of package contents
  const packageHash = calculatePackageHash(packageJson);
  console.log(`Package hash (SHA256): ${packageHash}`);

  // Sign the hash with private key
  const sign = crypto.createSign('RSA-SHA256');
  sign.update(packageHash);
  sign.end();

  try {
    const signature = sign.sign(privateKey, 'base64');
    
    return {
      signature: signature,
      keyId: keyId,
      algorithm: SIGNATURE_ALGORITHM,
      signedAt: new Date().toISOString(),
      packageHash: packageHash,
    };
  } catch (err) {
    console.error('Failed to sign package:', err.message);
    console.error('Make sure you are using a valid RSA private key in PEM format');
    process.exit(1);
  }
}

/**
 * Main function
 */
function main() {
  const options = parseArgs();

  if (options.help) {
    showHelp();
    return;
  }

  console.log('Unity Package Signing Tool\n');

  // Read package.json
  if (!fs.existsSync(PACKAGE_JSON_PATH)) {
    console.error(`package.json not found at: ${PACKAGE_JSON_PATH}`);
    process.exit(1);
  }

  const packageJson = JSON.parse(fs.readFileSync(PACKAGE_JSON_PATH, 'utf-8'));
  console.log(`Package: ${packageJson.name} v${packageJson.version}`);

  let newSignature;
  let signatureType;

  // Determine signing method
  const hasPrivateKey = options.privateKeyPath || process.env.UNITY_PACKAGE_PRIVATE_KEY;
  const hasKeyId = options.keyId || process.env.UNITY_PACKAGE_KEY_ID;

  if (hasPrivateKey && hasKeyId) {
    // Sign with private key
    console.log('\n[Signing with private key]');
    signatureType = 'cryptographic';
    newSignature = signPackage(options.privateKeyPath, options.keyId, packageJson);
    console.log(`Signature algorithm: ${newSignature.algorithm}`);
    console.log(`Key ID: ${newSignature.keyId}`);
    console.log(`Signed at: ${newSignature.signedAt}`);
    console.log(`Signature (truncated): ${newSignature.signature.substring(0, 64)}...`);
  } else {
    // Mark as unsigned
    console.log('\n[Marking package as unsigned]');
    console.log('No private key provided - marking package as "unsigned"');
    console.log('This will suppress Unity 6.3+ warnings about missing signatures.');
    signatureType = 'unsigned';
    newSignature = 'unsigned';
  }

  // Update package.json
  packageJson.signature = newSignature;

  if (options.dryRun) {
    console.log('\n[DRY RUN] Would update package.json with:');
    console.log(JSON.stringify({ signature: newSignature }, null, 2));
    console.log('\nNo changes made (dry run mode)');
  } else {
    // Write updated package.json
    fs.writeFileSync(
      PACKAGE_JSON_PATH,
      JSON.stringify(packageJson, null, 2) + '\n',
      'utf-8'
    );
    console.log(`\n✓ Successfully updated package.json with ${signatureType} signature`);
    console.log(`  Location: ${PACKAGE_JSON_PATH}`);
  }

  console.log('\nDistribution Compatibility:');
  console.log('  ✓ OpenUPM');
  console.log('  ✓ NPM');
  console.log('  ✓ Git URLs');
  console.log('  ✓ Manual Export (.unitypackage)');
  console.log('  ✓ Unity 2021.3+ (backwards compatible)');
  console.log('  ✓ Unity 6.3+ (suppresses warning)');
}

// Run the script
if (require.main === module) {
  main();
}

module.exports = { calculatePackageHash, signPackage };
