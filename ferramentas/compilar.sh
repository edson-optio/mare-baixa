#!/bin/sh
# Compila o projeto Unity em batchmode (Maré Baixa → 1. Montar projeto) e mostra só os erros de C#.
# Usa uma trava: o Unity não abre o mesmo projeto duas vezes, e vários agentes compilam em paralelo.
#
# Uso: sh ferramentas/compilar.sh          (monta o projeto)
#      sh ferramentas/compilar.sh build    (monta e gera Entrega/MareBaixa.app)
# Sai com código 1 se houver erro de compilação ou se o Unity falhar.

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
UNITY=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity
TRAVA="${TMPDIR:-/tmp}/mare-baixa-unity.lock"
LOG="$RAIZ/Verificacao/unity-compilar.log"
METODO=MareBaixa.Editor.ProjectBuilder.CreateProject
[ "$1" = "build" ] && METODO=MareBaixa.Editor.ProjectBuilder.BuildAll

espera=0
until mkdir "$TRAVA" 2>/dev/null; do
  [ $espera -eq 0 ] && echo "Outro agente está compilando; esperando a trava ($TRAVA)..."
  espera=$((espera + 5))
  if [ $espera -ge 900 ]; then echo "Trava presa há 15 min. Se ninguém estiver compilando: rmdir $TRAVA"; exit 1; fi
  sleep 5
done
trap 'rmdir "$TRAVA" 2>/dev/null' EXIT INT TERM

mkdir -p "$RAIZ/Verificacao"
"$UNITY" -batchmode -quit -projectPath "$RAIZ/MareBaixa" -executeMethod "$METODO" -logFile "$LOG"
codigo=$?

erros=$(grep -E "error CS[0-9]+|Exception:|Falha no build" "$LOG" | sort -u)
if [ -n "$erros" ] || [ $codigo -ne 0 ]; then
  echo "FALHOU (código $codigo). Erros:"
  echo "$erros" | head -40
  echo "Log completo: $LOG"
  exit 1
fi
grep -E "MARE_PROJECT_READY|MARE_BUILD_READY" "$LOG" | tail -2
echo "OK: compilou sem erros."
