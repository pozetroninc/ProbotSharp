#!/usr/bin/env bash
set -euo pipefail

if ! command -v git >/dev/null 2>&1; then
  echo "kubeconform pre-commit hook requires git" >&2
  exit 1
fi
if ! command -v curl >/dev/null 2>&1; then
  echo "kubeconform pre-commit hook requires curl" >&2
  exit 1
fi
if ! command -v tar >/dev/null 2>&1; then
  echo "kubeconform pre-commit hook requires tar" >&2
  exit 1
fi

ROOT_DIR="$(git rev-parse --show-toplevel)"
cd "$ROOT_DIR"

mapfile -t K8S_FILES < <(git diff --cached --name-only --diff-filter=ACM |
  grep -E '[.]ya?ml$' |
  grep -E '^deploy/(k8s|kubernetes)/' |
  grep -v -E '^deploy/(k8s|kubernetes)/helm/' |
  grep -v -E 'kustomization\.ya?ml$' || true)

if [[ ${#K8S_FILES[@]} -eq 0 ]]; then
  echo "kubeconform: no Kubernetes YAML changes staged; skipping."
  exit 0
fi

resolve_kubeconform() {
  if [[ -n "${KUBECONFORM_BIN:-}" ]]; then
    echo "$KUBECONFORM_BIN"
    return 0
  fi

  if command -v kubeconform >/dev/null 2>&1; then
    command -v kubeconform
    return 0
  fi

  local install_dir="$ROOT_DIR/tools/kubeconform"
  local binary_path="$install_dir/kubeconform"

  if [[ -x "$binary_path" ]]; then
    echo "$binary_path"
    return 0
  fi

  mkdir -p "$install_dir"
  download_kubeconform "$install_dir"
  echo "$binary_path"
}

download_kubeconform() {
  local install_dir="$1"
  local version="0.6.7"

  local os
  os="$(uname -s)"
  local arch
  arch="$(uname -m)"

  local os_segment
  case "$os" in
    Linux) os_segment="linux" ;;
    Darwin) os_segment="darwin" ;;
    *)
      echo "kubeconform: unsupported OS '$os'. Please install kubeconform and re-run." >&2
      exit 1
      ;;
  esac

  local arch_segment
  case "$arch" in
    x86_64|amd64) arch_segment="amd64" ;;
    arm64|aarch64) arch_segment="arm64" ;;
    *)
      echo "kubeconform: unsupported architecture '$arch'. Please install kubeconform manually." >&2
      exit 1
      ;;
  esac

  local filename="kubeconform-${os_segment}-${arch_segment}.tar.gz"
  local url="https://github.com/yannh/kubeconform/releases/download/v${version}/${filename}"
  local tmpfile
  tmpfile="$(mktemp)"

  echo "kubeconform: downloading ${url}" >&2
  if ! curl -sSL -o "$tmpfile" "$url"; then
    echo "kubeconform: failed to download binary" >&2
    rm -f "$tmpfile"
    exit 1
  fi

  if ! tar -xzf "$tmpfile" -C "$install_dir" kubeconform; then
    echo "kubeconform: failed to extract archive" >&2
    rm -f "$tmpfile"
    exit 1
  fi

  rm -f "$tmpfile"
  chmod +x "$install_dir/kubeconform"
}

KUBECONFORM_BIN_PATH="$(resolve_kubeconform)"
SCHEMA_DIR="$ROOT_DIR/tools/kubeconform/schemas"
SCHEMA_TEMPLATE="$SCHEMA_DIR/{{ .NormalizedKubernetesVersion }}-standalone{{ .StrictSuffix }}/{{ .ResourceKind }}{{ .KindSuffix }}.json"

if [[ ! -d "$SCHEMA_DIR" ]]; then
  echo "kubeconform: expected schema cache at '$SCHEMA_DIR' not found" >&2
  echo "kubeconform: ensure you have pulled the repository's schema assets." >&2
  exit 1
fi

for path in "${K8S_FILES[@]}"; do
  if [[ ! -f "$path" ]]; then
    echo "kubeconform: staged file '$path' no longer exists" >&2
    exit 1
  fi
done

echo "kubeconform: validating ${#K8S_FILES[@]} file(s)"
"$KUBECONFORM_BIN_PATH" \
  -schema-location "$SCHEMA_TEMPLATE" \
  -strict \
  "${K8S_FILES[@]}"
