# Operation Zero

Projeto Unity 6 baseado no arquivo REV07. Esta revisão prepara o fluxo offline para validação no Unity e no Android. Consulte o [relatório de validação](VALIDACAO.md) para ver o que foi testado e o que falta.

## Abrir o projeto

Use o Unity 6000.0.38f1. Para Android, instale também Android Build Support, Android SDK & NDK Tools e OpenJDK no Unity Hub. Os pacotes URP e de UI são resolvidos pelo Package Manager.

1. Abra a raiz do projeto no Unity e aguarde a importação.
2. Use **Operation Zero → Preparar cena** para criar e abrir `Assets/Scenes/OperationZero.unity`. O gerador usa os recursos incluídos e também reconhece os prefabs autorais `Island.prefab`, `Soldier.prefab` e `Rifle.prefab` da estrutura REV07 quando presentes.
3. Clique em Play e selecione **Battle Royale**, **Combate** ou **Treinamento**. Confira se **Active Input Handling** está em **Input Manager (Old)** ou **Both** se tiver mudado essa configuração.

No Editor, use WASD, botão direito + mouse para olhar, botão esquerdo para disparar, Shift para correr, Ctrl para agachar, Espaço para pular, C para deslizar, R para recarregar, E para coletar e H para usar um kit. No Android, o lado esquerdo controla o deslocamento, a área central direita controla a câmera e o HUD contém as ações. Sensibilidade, qualidade e limite de 30/60 FPS são ajustáveis no lobby.

O modo BR começa com descida controlável, 12 bots e fase de espera antes de a zona fechar. Há loot de munição, colete, capacete e kits. Os quatro perfis de armas compartilham o modelo visual do rifle e o som. O terreno gerado mede 320 × 320 m e tem via, armazéns, barreiras e rochas com dois LODs. Os clipes de combate incluídos são de pistola e precisam de ajuste visual para a empunhadura do rifle.

## Android

Execute `bash Tools/build_android.sh` com o Unity licenciado e `UNITY_EDITOR` apontando para o executável. O script faz a auditoria de conteúdo e chama o editor para gerar o APK de desenvolvimento ARM64/IL2CPP em `Builds/OperationZero-debug.apk`. Depois de um build bem-sucedido, a checagem verifica o manifesto, o DEX e as bibliotecas nativas.

O workflow manual **Android development build** usa GameCI e as credenciais configuradas nos secrets do repositório. Ele publica o APK como artefato somente após a compilação e a checagem. Nenhum APK foi produzido nesta revisão.

## Servidor legado

`LegacyServer/` contém o servidor e cliente web da REV07. Para executar localmente, instale `requirements.txt` e inicie `server.py`. O cliente Unity usa a API HTTPS para cadastro, login e ranking. As partidas locais do Unity gravam XP no dispositivo. O servidor legado mantém suas próprias partidas e ranking.

A integração multiplayer nativa depende de sincronização, lobby e validação do mapa Unity. Os testes do servidor legado cobrem apenas o protocolo e as regras do servidor.

## Recursos

O projeto inclui os arquivos licenciados usados no fluxo offline. Veja [fontes e licenças](LICENCAS-ASSETS.md). `python3 Tools/preflight.py` valida o conteúdo sem acesso à rede.
