#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# compile.sh
#
# Compiles the Unity package by opening the test project and letting Unity
# resolve and compile all scripts.
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
# Flags:
#   --clean  Force recreation of the test project before compiling
#
# Usage:
#   ./compile.sh
#   ./compile.sh --clean
###############################################################################

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

CLEAN_FLAG=""
for arg in "$@"; do
    case "${arg}" in
        --clean)
            CLEAN_FLAG="--force"
            ;;
        *)
            echo "WARNING: Unknown argument: ${arg}"
            ;;
    esac
done

echo "==> [compile] Starting Unity compilation..."
START_TIME=$(date +%s)

# Step 1: Ensure test project exists
echo "==> [compile] Step 1: Ensuring test project exists..."
if [[ -n "${CLEAN_FLAG}" ]]; then
    "${SCRIPT_DIR}/create-test-project.sh" --force
else
    "${SCRIPT_DIR}/create-test-project.sh"
fi

# Step 2: Run Unity compilation
echo "==> [compile] Step 2: Running Unity compilation..."
EXIT_CODE=0
"${SCRIPT_DIR}/run-unity-docker.sh" \
    -batchmode -nographics -quit \
    -projectPath /project \
    -logFile - || EXIT_CODE=$?

END_TIME=$(date +%s)
ELAPSED=$((END_TIME - START_TIME))

if [[ "${EXIT_CODE}" -eq 0 ]]; then
    echo "==> [compile] Compilation succeeded in ${ELAPSED}s."
else
    echo "==> [compile] Compilation FAILED in ${ELAPSED}s (exit code: ${EXIT_CODE})."
fi

exit "${EXIT_CODE}"
