#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLUSTER_NAME="crypto-shredding"

# kubectl wait falha imediatamente com "no matching resources found" se os pods
# ainda não existirem no instante da chamada (a criação pelo controller é assíncrona
# em relação ao kubectl apply). Por isso repetimos até o selector encontrar algo.
wait_for_pods() {
  local namespace="$1"
  local selector="$2"
  local timeout="${3:-180}"
  local deadline=$((SECONDS + timeout))
  local target=(--all)
  if [ -n "$selector" ]; then
    target=(--selector="$selector")
  fi

  until kubectl -n "$namespace" wait --for=condition=ready pod "${target[@]}" --timeout=10s >/dev/null 2>&1; do
    if [ "$SECONDS" -ge "$deadline" ]; then
      echo "Timeout esperando pods prontos em '$namespace' ($selector)" >&2
      kubectl -n "$namespace" get pods
      exit 1
    fi
    sleep 2
  done
}

if ! kind get clusters 2>/dev/null | grep -qx "$CLUSTER_NAME"; then
  echo "==> Criando cluster kind '$CLUSTER_NAME'"
  kind create cluster --config "$ROOT_DIR/kind-config.yaml"
else
  echo "==> Cluster kind '$CLUSTER_NAME' já existe, reaproveitando"
fi

echo "==> Instalando ingress-nginx"
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml

echo "==> Aguardando o ingress-nginx ficar pronto"
wait_for_pods ingress-nginx app.kubernetes.io/component=controller 180

"$ROOT_DIR/scripts/build.sh"

echo "==> Aplicando manifests do Kubernetes"
kubectl apply -f "$ROOT_DIR/k8s/"

echo "==> Aguardando os pods da aplicação ficarem prontos"
wait_for_pods crypto-shredding "" 300

echo
echo "Tudo pronto! Acesse http://localhost"
echo "Para ver a documentação Scalar da API (fora do ingress): kubectl -n crypto-shredding port-forward svc/customers-api 8080:8080 e acesse http://localhost:8080/scalar"
