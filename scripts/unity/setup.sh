#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# setup.sh
#
# One-time setup script that prepares the environment for Unity testing.
# Checks Docker availability, pulls the Unity Docker image, creates the test
# project structure, and verifies by running a quick Unity version check.
#
# Environment variables:
#   UNITY_VERSION          - Unity Editor version (default: 2021.3.45f1)
#   UNITY_IMAGE_VERSION    - GameCI image version (default: 3)
#   UNITY_LICENSE          - Contents of .ulf license file
#   UNITY_SERIAL           - Pro license serial key
#   UNITY_EMAIL            - Unity account email
#   UNITY_PASSWORD         - Unity account password
#   UNITY_TEST_PROJECT_DIR - Path to test project (default: /home/vscode/.unity-test-project)
#
# Usage:
#   ./setup.sh
###############################################################################

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

UNITY_VERSION="${UNITY_VERSION:-2021.3.45f1}"
UNITY_IMAGE_VERSION="${UNITY_IMAGE_VERSION:-3}"
UNITY_IMAGE="unityci/editor:ubuntu-${UNITY_VERSION}-base-${UNITY_IMAGE_VERSION}"

echo "==> [setup] Starting Unity environment setup..."
START_TIME=$(date +%s)

# Step 1: Check Docker availability
echo "==> [setup] Step 1: Checking Docker availability..."
if ! docker info > /dev/null 2>&1; then
    echo "ERROR: Docker is not available. Please install Docker or start the Docker daemon."
    exit 1
fi
echo "    Docker is available."

# Step 2: Pull Unity Docker image if not present
echo "==> [setup] Step 2: Pulling Unity Docker image (${UNITY_IMAGE})..."
if docker image inspect "${UNITY_IMAGE}" > /dev/null 2>&1; then
    echo "    Image already exists locally. Skipping pull."
else
    echo "    Pulling image (this may take a while)..."
    docker pull "${UNITY_IMAGE}"
    echo "    Image pulled successfully."
fi

# Step 3: Create test project structure
echo "==> [setup] Step 3: Creating test project structure..."
"${SCRIPT_DIR}/create-test-project.sh"

# Step 4: Verify setup with Unity version check
echo "==> [setup] Step 4: Verifying Unity installation..."
"${SCRIPT_DIR}/run-unity-docker.sh" \
    -batchmode -nographics -quit \
    -logFile - \
    -version

END_TIME=$(date +%s)
ELAPSED=$((END_TIME - START_TIME))

echo ""
echo "==> [setup] Setup complete in ${ELAPSED}s."
echo "    Unity image: ${UNITY_IMAGE}"
echo "    Unity version: ${UNITY_VERSION}"
echo "    Test project: ${UNITY_TEST_PROJECT_DIR:-/home/vscode/.unity-test-project}"
echo ""
echo "    Next steps:"
echo "      - Compile: ./scripts/unity/compile.sh"
echo "      - Run tests: ./scripts/unity/run-tests.sh"
echo "      - Run tests (all): ./scripts/unity/run-tests.sh --mode all"
