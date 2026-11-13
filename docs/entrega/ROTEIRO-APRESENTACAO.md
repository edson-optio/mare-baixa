# Roteiro da apresentação oral (≈ 15 min)

Formato: slides do manual + jogo rodando ao vivo. Deixe o jogo aberto antes de começar, com um save que já
tenha pérolas e um banco perto do Ermitão Colosso (assim a demo não depende de sorte).

Ajuste quem fala cada bloco; a divisão abaixo é só uma sugestão.

| Tempo | Bloco | Quem | O que mostrar |
|---|---|---|---|
| 0:00 – 1:00 | Abertura | Integrante 1 | Capa do manual. Nome do jogo, frase "A maré está subindo. Escolha bem a sua concha.", equipe |
| 1:00 – 3:00 | A ideia | Integrante 1 | Página 2 do manual: história (Polvo roubou a Pérola Lunar), objetivo, loop vila → zonas → chefe → vila |
| 3:00 – 5:00 | O que torna o jogo diferente | Integrante 2 | Página 5: a concha é a classe e a vida. Página 7: maré que sobe e ar que acaba. Pilares: decisões sob pressão, sempre se adaptar, morrer ensina, humor com mensagem |
| 5:00 – 6:00 | Como se joga | Integrante 2 | Página 3 (controles) e página 4 (HUD) |
| 6:00 – 11:00 | **Demo ao vivo** | Integrante 3 joga, Integrante 2 narra | Ver roteiro abaixo |
| 11:00 – 13:00 | Como foi feito | Integrante 1 | Unity 6.3, salas em texto sorteadas, UI por código, pixel art, som sintetizado, autoteste que joga sozinho e tira capturas. O que mudou do GDD para o jogo |
| 13:00 – 14:00 | Manual e entregáveis | Integrante 3 | Página 9 (três principais cenas), links do GitHub, YouTube e itch.io |
| 14:00 – 15:00 | Encerramento e perguntas | Todos | Página 10 (mensagem sobre poluição) |

## Roteiro da demo (5 min)

1. **Menu → Novo jogo** (20 s): mostrar a arte do menu e a cutscene 1 (pular depois de 2 quadros, segurando Esc).
2. **Vila da Praia** (60 s): falar com a Vovó Búzia; abrir a loja da Ostrácia e comprar o Pulmão de Peixe; abrir a
   forja da Dona Lagosta e trocar a concha inicial para a Tampinha (mostrar o pulo duplo).
3. **Recife Raso** (90 s): pegar a Espada de Canudo na entrada; lutar com a Água-viva e o Baiacu; deixar a água
   subir para mostrar o som abafado e a barra de ar; pegar uma bolha.
4. **Troca de concha** (30 s): vestir uma concha nova (cutscene 2 aparece na primeira vez) e mostrar que a antiga
   fica no chão.
5. **Banco e chefe** (60 s): continuar do banco antes do Ermitão Colosso; mostrar que ele fica tonto ao bater na
   parede e atacar.
6. **Fecho** (20 s): voltar ao menu pela pausa. Se der tempo, mostrar a Fossa escura com a Lâmpada.

Plano B: se algo travar, mostre o vídeo do YouTube a partir do trecho correspondente.

## Perguntas prováveis

- **Por que roguelite?** Runs curtas e progressão permanente fazem cada derrota valer alguma coisa (pilar "morrer ensina").
- **Como as salas são sorteadas?** Cada zona tem um banco de salas feitas à mão em arquivos de texto; a run sorteia
  algumas e ordena por dificuldade (`@nivel`). Entrada e arena do chefe são fixas.
- **O que ficou fora?** A faixa Extra do GDD: os modos Maré Viva e Desafio Diário.
- **Como testaram?** O jogo tem um modo `-selfTest` que joga sozinho o loop inteiro, verifica 150 regras e tira capturas
  de todas as salas, chefes e cutscenes. Além disso, playtests com o grupo.
