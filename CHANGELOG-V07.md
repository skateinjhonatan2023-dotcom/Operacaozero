# Changelog — 0.7 DEV (não validada)
- Lobby: rotação do operador por gesto e centralização, enquadramento de corpo inteiro, piso físico PBR, fallback de importação.
- Gameplay: orientação relativa à câmera, postura protegida contra teto, cancelamento de deslize em colisões, ADS independente, troca de ombro.
- HUD: safe area, captura independente de três dedos, limpeza em pausa.
- FPS: corpo local renderizado apenas em sombras durante FPS; arma estática mantida.
- Editor: validação adicional de Avatar, malha e animação de lobby na cena gerada.
- Sem novas dependências nem alterações no workflow.
- Testes Unity, APK, novos modelos e mapa ainda pendentes. Consulte VALIDACAO-V07.md.
## Segunda etapa — implementação sem build
- Ilha Zero: terreno 2000 × 2000 m, dez distritos em código, vias conformadas ao terreno, armazéns, janelas, portas, vegetação e mar procedurais, rochas licenciadas existentes.
- Offline: até 20 participantes, zona escalada, suprimentos periódicos, alvos de treinamento passivos, bots com patrulha, zona, recarga e níveis de dificuldade.
- HUD: sobreviventes, bússola numérica, hitmarker, direção do dano, botão de interação, minimapa escalado sem revelar bots.
- Veículos: seis instâncias de três protótipos procedurais; entrada/saída, direção, aceleração, frenagem e colisão simplificada. Sem modelos ou sons finais.
- Combate: escopeta experimental (modelo de rifle reaproveitado), granadas, disparo semiautomático e recuo por categoria.
- Configurações: corrida automática, giroscópio quando suportado, FOV, suavização e dificuldade.
- Nenhum teste executado nesta etapa por solicitação do responsável. Nenhuma build solicitada.

## V0.7.1 — Correções do teste Android da Build 7
- Feedback real: corpos enterrados, mapa visualmente vazio e HUD de botões retangulares; lobby/rotação 360° aprovados pelo jogador e preservados.
- Terreno gerado explicitamente, altura interpolada no espaço do terreno, collider sincronizado, renderização não instanciada, maior distância de visão, nova pintura e bairros mais densos. Spawn protegido e recuperação de cápsula abaixo do solo; alinhamento visual por ossos dos pés separado do controlador. Causa exata ainda precisa ser confirmada no dispositivo.
- HUD original inspirado na disposição enviada pelo jogador: círculos, ícones gerados no código, analógico móvel, mapa de alturas/estradas com zonas e marcadores, barras de HP/colete, inventário, mira e configurações. Sem falsa indicação de online.
- Referências de organização espacial (não importadas): Erangel — https://pubg.com/pt-br/game-info/maps/erangel ; Bermuda — https://play.google.com/store/apps/editorial?hl=en_US&id=mc_editorialevergreen_post_install_free_fire_map_spotlight_3_now_fcp .
- Arte continua procedural e modelos existentes; não é reprodução de PUBG/Free Fire. Nenhum novo asset externo importado. Não compilado nem testado no Android nesta etapa. APK V0.7.1 requer nova build.