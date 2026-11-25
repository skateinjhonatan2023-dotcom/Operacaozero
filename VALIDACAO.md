# Validação da revisão Unity

A base é o arquivo `OPERATION_ZERO_UNITY_REV07_CI_1_.zip`, importado no primeiro commit local. As mudanças foram feitas sobre os scripts, o editor de build e a estrutura da REV07. A cena, os materiais e os assets URP derivados são gerados pelo projeto no Unity.

## Testes executados

| Verificação | Resultado |
| --- | --- |
| `python3 Tools/preflight.py` | Passou. Arquivos de conteúdo passaram na verificação de tamanho e SHA-256. Os 11 nomes esperados foram encontrados no FBX de animações. |
| Importação e inspeção FBX no Blender 4.3.2 | Passou. Personagem com 65 ossos e três malhas com skin. O FBX de animação importou 43 clipes, incluindo os 11 usados. Rifle com uma malha, UVs e 5.159 vértices. Rochas com UVs e LODs de 4.500 e 948 triângulos. |
| Imagens e áudio | Passou. Pillow verificou os arquivos PNG e JPEG, incluindo os três pisos e superfícies 2K. A leitura WAV confirmou áudio mono em 44,1 kHz. |
| Análise estática do C# e dos arquivos de build | Passou. Os 12 scripts C# passaram na análise sintática por Tree-sitter. O workflow foi analisado como YAML e o script Android passou em `bash -n`. |
| `LegacyServer/test_server.py` | Passou. Cadastro, rejeição de senha incorreta, matchmaking, snapshots de dois clientes, eliminação, respawn e ranking. |
| `LegacyServer/test_br.py` | Passou. Fila BR com dois clientes, contagem, eliminação sem respawn, vitória e ranking. |
| `LegacyServer/test_security.py` | Passou. Validação de sessão duplicada, munição, recarga e movimento do servidor legado. |
| `bash Tools/build_android.sh` | Bloqueado na detecção do Unity Editor, saída 2. A compilação C#, o IL2CPP e o empacotamento APK ainda precisam ser executados em um Unity licenciado com módulos Android. |

## Pendências

- Compilar e importar no Unity. Validar Avatar, retarget, referências da cena, shaders, cores e empunhadura. A importação no Blender cobre a estrutura dos arquivos FBX.
- Testar partidas no Unity com teclado e mouse e em Android com multitouch, incluindo movimentação, câmera, salto, agachamento, disparo, recarga, loot, cura, bots, zona e telas de resultado.
- Validar cadastro, login e ranking pelo cliente Unity em um servidor HTTPS. O multiplayer nativo, as equipes, amigos, histórico, ranking global e reconexão permanecem pendentes.
- Fazer a revisão visual em execução e produzir vegetação, modelos distintos de armas, animação com rifle, personalização do HUD e minimapa com representação do terreno. A cena usa geometria simples para armazéns e barreiras. Os objetos foram marcados para oclusão, mas o bake de oclusão depende do Unity.
- Compilar, assinar, instalar e testar o APK. Medir FPS, memória e temperatura no OPPO A06 ou em aparelho equivalente. O workflow GameCI ainda precisa ser executado com a licença nos secrets.
- Publicar os commits e abrir o PR. A integração do GitHub retornou HTTP 404 para o repositório e o Git retornou HTTP 403 na consulta de referências. O histórico local foi criado a partir do ZIP.
