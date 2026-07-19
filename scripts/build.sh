#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLUSTER_NAME="crypto-shredding"

echo "==> Building customers-api:dev"
docker build -t customers-api:dev -f "$ROOT_DIR/CryptoShredding/Customers/Customers.Api/Dockerfile" "$ROOT_DIR/CryptoShredding"

echo "==> Building orders-api:dev"
docker build -t orders-api:dev -f "$ROOT_DIR/CryptoShredding/Orders/Orders.Api/Dockerfile" "$ROOT_DIR/CryptoShredding"

echo "==> Building web:dev"
docker build -t web:dev -f "$ROOT_DIR/CryptoShredding/web/Dockerfile" "$ROOT_DIR/CryptoShredding/web"

echo "==> Loading images into kind cluster '$CLUSTER_NAME'"
kind load docker-image customers-api:dev --name "$CLUSTER_NAME"
kind load docker-image orders-api:dev --name "$CLUSTER_NAME"
kind load docker-image web:dev --name "$CLUSTER_NAME"
