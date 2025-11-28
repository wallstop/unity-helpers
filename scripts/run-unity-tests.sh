#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEFAULT_PROJECT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"
DEFAULT_RESULTS="${SCRIPT_DIR}/../artifacts/unity-tests"

UNITY_PATH="${UNITY_PATH:-${UNITY_EDITOR_PATH:-}}"
PROJECT_PATH="${PROJECT_PATH:-${UNITY_PROJECT_PATH:-${DEFAULT_PROJECT}}}"
RESULTS_DIR="${RESULTS_DIR:-${UNITY_TEST_RESULTS_DIR:-${DEFAULT_RESULTS}}}"

if [[ -z "$UNITY_PATH" ]]; then
  echo "Unity editor path not provided. Set UNITY_EDITOR_PATH or UNITY_PATH." >&2
  exit 1
fi

if [[ ! -f "$UNITY_PATH" ]]; then
  echo "Unity editor not found at $UNITY_PATH" >&2
  exit 1
fi

if [[ ! -d "$PROJECT_PATH" ]]; then
  echo "Unity project path $PROJECT_PATH does not exist." >&2
  exit 1
fi

mkdir -p "$RESULTS_DIR"
echo "Unity Editor: $UNITY_PATH"
echo "Project Path: $PROJECT_PATH"
echo "Results Directory: $RESULTS_DIR"

PLATFORMS=()
if [[ $# -gt 0 ]]; then
  PLATFORMS=("$@")
else
  PLATFORMS=("EditMode" "PlayMode")
fi

if [[ ${#PLATFORMS[@]} -eq 0 ]]; then
  echo "Specify at least one platform (EditMode / PlayMode)." >&2
  exit 1
fi

run_platform() {
  local platform="$1"
  local result_file="${RESULTS_DIR}/${platform}-TestResults.xml"
  local log_file="${RESULTS_DIR}/${platform}.log"

  local args=(
    "-batchmode"
    "-quit"
    "-projectPath" "$PROJECT_PATH"
    "-runTests"
    "-testPlatform" "$platform"
    "-testResults" "$result_file"
    "-logFile" "$log_file"
  )

  if [[ -n "${UNITY_TEST_FILTER:-}" ]]; then
    args+=("-testFilter" "$UNITY_TEST_FILTER")
  fi
  if [[ -n "${UNITY_TEST_CATEGORY:-}" ]]; then
    args+=("-testCategory" "$UNITY_TEST_CATEGORY")
  fi
  if [[ -n "${UNITY_TEST_EXTRA_ARGS:-}" ]]; then
    # shellcheck disable=SC2206
    extra_args=(${UNITY_TEST_EXTRA_ARGS})
    args+=("${extra_args[@]}")
  fi

  echo
  echo "Running Unity tests for ${platform}..."
  echo "$UNITY_PATH ${args[*]}"

  if ! "$UNITY_PATH" "${args[@]}"; then
    echo "Unity exited with an error for ${platform}. See ${log_file}" >&2
    exit 1
  fi
}

for platform in "${PLATFORMS[@]}"; do
  run_platform "$platform"
done

echo
echo "Unity tests completed successfully."
