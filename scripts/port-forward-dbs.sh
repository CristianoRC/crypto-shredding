#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="crypto-shredding"

pids=()
cleanup() {
  echo
  echo "==> Encerrando port-forwards"
  kill "${pids[@]}" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

forward() {
  local service="$1"
  local local_port="$2"
  local remote_port="$3"
  echo "==> $service disponível em localhost:$local_port"
  kubectl -n "$NAMESPACE" port-forward "svc/$service" "$local_port:$remote_port" >/dev/null 2>&1 &
  pids+=($!)
}

forward postgres-customers 5432 5432
forward postgres-customers-keys 5433 5432
forward postgres-orders 5434 5432
forward redis 6379 6379

echo
echo "Postgres (customersdb): postgres://postgres:postgres@localhost:5432/customersdb"
echo "Postgres (keyvault):    postgres://postgres:postgres@localhost:5433/keyvault"
echo "Postgres (ordersdb):    postgres://postgres:postgres@localhost:5434/ordersdb"
echo "Redis:                  localhost:6379"
echo
echo "Ctrl+C para encerrar."

wait
