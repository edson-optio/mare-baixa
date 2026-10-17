#!/bin/zsh
set -e
DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
EDITOR_APP='/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app'
if [[ ! -d "$EDITOR_APP" ]]; then
  print 'Unity 6000.3.24f1 não encontrado. Abra o Unity Hub e adicione a pasta MareBaixa.'
  open -a 'Unity Hub'
  exit 1
fi
open -a "$EDITOR_APP" --args -projectPath "$DIR/MareBaixa"
