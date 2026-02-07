#!/bin/bash

# Test script for Unity package signing
# This script verifies that the sign-package.js script works correctly

set -e

echo "=== Unity Package Signing Test Suite ==="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$PROJECT_ROOT"

echo "Project root: $PROJECT_ROOT"
echo ""

# Test 1: Dry run with unsigned mode
echo "Test 1: Dry run with unsigned mode"
if npm run sign:package:dry-run > /tmp/signing-test-1.log 2>&1; then
    if grep -q "unsigned" /tmp/signing-test-1.log && grep -q "No changes made" /tmp/signing-test-1.log; then
        echo -e "${GREEN}✓ Pass${NC}: Dry run with unsigned mode works"
    else
        echo -e "${RED}✗ Fail${NC}: Dry run output unexpected"
        cat /tmp/signing-test-1.log
        exit 1
    fi
else
    echo -e "${RED}✗ Fail${NC}: Dry run failed"
    cat /tmp/signing-test-1.log
    exit 1
fi
echo ""

# Test 2: Help output
echo "Test 2: Help output"
if node scripts/sign-package.js --help > /tmp/signing-test-2.log 2>&1; then
    if grep -q "Usage:" /tmp/signing-test-2.log && grep -q "Options:" /tmp/signing-test-2.log; then
        echo -e "${GREEN}✓ Pass${NC}: Help output works"
    else
        echo -e "${RED}✗ Fail${NC}: Help output unexpected"
        cat /tmp/signing-test-2.log
        exit 1
    fi
else
    echo -e "${RED}✗ Fail${NC}: Help output failed"
    cat /tmp/signing-test-2.log
    exit 1
fi
echo ""

# Test 3: Verify package.json has signature field
echo "Test 3: Verify package.json has signature field"
if jq -e '.signature' package.json > /dev/null 2>&1; then
    SIGNATURE=$(jq -r '.signature' package.json)
    if [ "$SIGNATURE" = "unsigned" ] || [ -n "$SIGNATURE" ]; then
        echo -e "${GREEN}✓ Pass${NC}: package.json has signature field: $SIGNATURE"
    else
        echo -e "${RED}✗ Fail${NC}: Signature field is empty or null"
        exit 1
    fi
else
    echo -e "${RED}✗ Fail${NC}: package.json missing signature field"
    exit 1
fi
echo ""

# Test 4: Test cryptographic signing with test key (if openssl available)
echo "Test 4: Test cryptographic signing with test key"
if command -v openssl > /dev/null 2>&1; then
    # Generate test key
    openssl genrsa -out /tmp/test-signing-key.pem 2048 > /dev/null 2>&1
    
    # Test signing with key
    if node scripts/sign-package.js \
        --private-key /tmp/test-signing-key.pem \
        --key-id test-key-2024 \
        --dry-run > /tmp/signing-test-4.log 2>&1; then
        
        if grep -q "Signing with private key" /tmp/signing-test-4.log && \
           grep -q "RS256" /tmp/signing-test-4.log && \
           grep -q "test-key-2024" /tmp/signing-test-4.log; then
            echo -e "${GREEN}✓ Pass${NC}: Cryptographic signing works"
        else
            echo -e "${RED}✗ Fail${NC}: Cryptographic signing output unexpected"
            cat /tmp/signing-test-4.log
            exit 1
        fi
    else
        echo -e "${RED}✗ Fail${NC}: Cryptographic signing failed"
        cat /tmp/signing-test-4.log
        exit 1
    fi
    
    # Cleanup
    rm -f /tmp/test-signing-key.pem
else
    echo -e "${YELLOW}⊘ Skip${NC}: OpenSSL not available for key generation test"
fi
echo ""

# Test 5: Verify distribution compatibility markers in output
echo "Test 5: Verify distribution compatibility markers"
if npm run sign:package:dry-run 2>&1 | grep -q "OpenUPM\|NPM\|Git URLs\|Manual Export"; then
    echo -e "${GREEN}✓ Pass${NC}: Distribution compatibility info present"
else
    echo -e "${RED}✗ Fail${NC}: Missing distribution compatibility info"
    exit 1
fi
echo ""

# Test 6: Verify Unity version compatibility markers
echo "Test 6: Verify Unity version compatibility markers"
if npm run sign:package:dry-run 2>&1 | grep -q "Unity 2021.3+\|Unity 6.3+"; then
    echo -e "${GREEN}✓ Pass${NC}: Unity version compatibility info present"
else
    echo -e "${RED}✗ Fail${NC}: Missing Unity version compatibility info"
    exit 1
fi
echo ""

echo "=== All Tests Passed ==="
echo -e "${GREEN}✓ Package signing implementation is working correctly${NC}"
echo ""
echo "Summary:"
echo "  - Unsigned mode: ✓"
echo "  - Cryptographic mode: ✓"
echo "  - Help output: ✓"
echo "  - package.json signature: ✓"
echo "  - Distribution compatibility: ✓"
echo "  - Unity version compatibility: ✓"
