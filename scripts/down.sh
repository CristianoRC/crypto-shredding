#!/usr/bin/env bash
set -euo pipefail

CLUSTER_NAME="crypto-shredding"

echo "==> Apagando cluster kind '$CLUSTER_NAME'"
kind delete cluster --name "$CLUSTER_NAME"
